#nullable disable

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Enums;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;
using PermitPro.Core.Models;

using PuppeteerSharp;
using PuppeteerSharp.Media;

using Scriban;

using System.Text;

namespace PermitPro.Core.Services;

public class PermitService : IPermitService
{
	private readonly ApplicationDbContext _dbContext;
	private readonly PTWSettings _ptwSettings;
	private readonly IWebHostEnvironment _webHostEnvironment;
	private readonly IMessageService _messageService;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly ICurrentUserService _currentUserService;
	private readonly ILogService _logService;
	private readonly INotificationPushService _pushService;

	public PermitService(
		ApplicationDbContext context
		, PTWSettings ptwSettings
		, IWebHostEnvironment webHostEnvironment
		, IMessageService messageService
		, IHttpContextAccessor httpContextAccessor
		, ICurrentUserService currentUserService
		, ILogService logService
		, INotificationPushService pushService)
	{
		_dbContext = context;
		_ptwSettings = ptwSettings;
		_webHostEnvironment = webHostEnvironment;
		_messageService = messageService;
		_httpContextAccessor = httpContextAccessor;
		_currentUserService = currentUserService;
		_logService = logService;
		_pushService = pushService;
	}


	public async Task CreateAsync()
	{
		var form = _httpContextAccessor.HttpContext.Request.Form;
		var company = Guid.Parse(form["CompanyId"]);
		var permitId = Guid.NewGuid();
		var maxNum = 0;

		var formData = (string)form["PermitForm"];
		var isDraft = (string)form["SubmissionType"] == "draft";

		//Get the last running number for permit, increase running number to 1
		var lastRunningNo = _dbContext.Permits
			.Include(f => f.Company)
			.OrderByDescending(f => f.CreatedWhen)
			.FirstOrDefault(f => f.Company.Id == company);

		if (lastRunningNo != null)
		{
			maxNum = lastRunningNo.RunningNumber;
		}

		maxNum++;

		// Create a new permit with posted values
		var jsonPermit = JObject.Parse(formData);

		if (jsonPermit != null)
		{
			var permitCerts = (JArray)jsonPermit["general"]["certificates"];

			// Get current available workflow
			var workflow = _dbContext.Workflows
				.Include(e => e.WorkflowCompany)
				.FirstOrDefault(wf => wf.IsActive && wf.WorkflowCompany.Id == company && wf.HasCertificate == (permitCerts.Count > 0));

			if (workflow != null)
			{
				var locationId = (string)jsonPermit["general"]["location"]["id"];
				var loc = _dbContext.Sites.FirstOrDefault(f => f.Id.ToString().ToLower() == locationId);

				var userCompany = _dbContext.Companies.SingleOrDefault(f => f.Id == company);

				var permit = new Permit
				{
					Id = permitId,
					PermitNo = string.Format("{0:000000}", maxNum),
					PermitForm = form["PermitForm"],
					RunningNumber = maxNum,
					PermitStatus = isDraft ? PermitStatusEnum.Draft : PermitStatusEnum.Pending,
					Site = loc,
					Company = userCompany,
					CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
					CreatedBy = Guid.Parse(_currentUserService.GetCurrentUser().Id),
				};

				_dbContext.Permits.Add(permit);
				_dbContext.SaveChanges();

				// Add audit log to database
				var currentUser = _currentUserService.GetCurrentUser();
				var fullName = $"{currentUser.FirstName} {currentUser.LastName}";
				var logMessage = $"A new permit [PTW{permit.PermitNo}] has been created by {fullName.Trim()} ({currentUser.Email}) ";
				logMessage += $"on {permit.CreatedWhen:dd/MM/yyyy hh:mm tt}.";

				await _logService.LogMessageAsync(LogTypeEnum.Information, "CREATE_PERMIT", logMessage, currentUser);

				// Upload any attached documents
				if (form.Files.Count > 0)
				{
					var folderName = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", "permits", permitId.ToString().ToLower());

					if (!Directory.Exists(folderName))
					{
						Directory.CreateDirectory(folderName);
					}

					foreach (var item in form.Files)
					{
						var file = new FileInfo(item.FileName);
						var filePath = Path.Combine(folderName, file.Name);

						using (var fs = new FileStream(filePath, FileMode.Create))
						{
							await item.CopyToAsync(fs);
							fs.Flush();
						}
					}

					string[] filePaths = Directory.GetFiles(folderName);

					foreach (var filePath in filePaths)
					{
						var fi = new FileInfo(filePath);

						var attachment = new Attachment
						{
							FileName = fi.Name,
							ContentType = fi.Extension.Remove(0),
							Permit = permit,
							FileSize = (int)fi.Length,
						};

						_dbContext.Attachments.Add(attachment);
					}
				}

				WorkflowStep workflowStep;

				if (!isDraft)
				{
					workflowStep = _dbContext.WorkflowSteps
						.Include(wfs => wfs.Approvers)
						.Include(wf => wf.WorkflowStepWorkflow)
						.Where(wfs => wfs.WorkflowStepWorkflow == workflow && wfs.Name != "Draft" && wfs.Name != "Completed")
						.OrderBy(wfs => wfs.StepOrder)
						.FirstOrDefault();
				}
				else
				{
					workflowStep = _dbContext.WorkflowSteps
						.Include(wfs => wfs.Approvers)
						.Include(wf => wf.WorkflowStepWorkflow)
						.Where(wfs => wfs.WorkflowStepWorkflow == workflow && wfs.Name == "Draft")
						.OrderBy(wfs => wfs.StepOrder)
						.FirstOrDefault();
				}


				if (!isDraft && workflowStep.Approvers.Count > 0)
				{
					// Get email template
					var htmlTemplate = File.ReadAllText(Path.Combine(_webHostEnvironment.WebRootPath, "templates", "html", "wf-pending-approval.html"));
					var template = Template.Parse(htmlTemplate);

					// Generate email + internal notification for each approver
					var permitNoCreate = string.Format("PTW{0:000000}", permit.RunningNumber);
					foreach (var approver in workflowStep.Approvers)
					{
						var approverName = $"{approver.FirstName} {approver.LastName}";
						var approvalLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/account/login?company={company}&entity=permits&id={permitId}&origin=email";
						var notifLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/{company}/permits/edit/{permitId}";

						var templateData = new
						{
							RecipientName = approverName,
							WorkflowName = workflow.Name,
							PermitNo = permitNoCreate,
							DateSubmitted = GeneralHelper.GetDateInTimeZone(permit.CreatedWhen).ToString("dd/MM/yyyy hh:mm tt"),
							ApprovalLink = approvalLink,
						};

						var renderedHtml = template.Render(templateData);
						await _messageService.SendEmailAsync(new EmailInfo
						{
							Name = approverName,
							Email = approver.Email,
							Subject = $"Pending approval task for {permitNoCreate}",
							Body = renderedHtml
						});

						_dbContext.Notifications.Add(new Notification
						{
							Title = "Approval Required",
							Message = $"You have a pending approval task for {permitNoCreate}.",
							Url = notifLink,
							IsRead = false,
							IsArchived = false,
							NotificationUser = approver,
						});
						await _pushService.PushAsync(approver.Id, "Approval Required", $"You have a pending approval task for {permitNoCreate}.");
					}
				}


				// Update workflow step in permit
				if (workflowStep.WorkflowStepWorkflow != null) permit.PermitWorkflow = workflowStep.WorkflowStepWorkflow;
				permit.PermitWorkflowStep = workflowStep;
				_dbContext.Permits.Update(permit);

				// Create a workflow history entry
				var wfh = new WorkflowHistory
				{
					Permit = permit,
					HistoryWorkflow = workflow,
					HistoryWorkflowStep = workflowStep,
					Status = isDraft ? WorkflowStatusEnum.Draft : WorkflowStatusEnum.Pending,
					Comments = isDraft ? "Permit's draft information has been successfully saved." : "Permit is now pending for approval.",
				};

				_dbContext.WorkflowHistories.Add(wfh);

				await _dbContext.SaveChangesAsync();
			}
			else
			{
				throw new Exception("Invalid workflow");
			}
		}
	}


	public async Task CreateAsync(Permit entity)
	{
		_dbContext.Permits.Add(entity);
		await _dbContext.SaveChangesAsync();
	}


	public async Task UpdateAsync(Permit entity)
	{
		_dbContext.Permits.Update(entity);
		await _dbContext.SaveChangesAsync();
	}


	public async Task UpdateAsync()
	{
		var form = _httpContextAccessor.HttpContext.Request.Form;
		var permitId = Guid.Parse(form["PermitId"]);
		var formData = (string)form["PermitForm"];
		var isDraft = (string)form["SubmissionType"] == "draft";
		var companyId = Guid.Parse(form["CompanyId"]);

		// Create a new permit with posted values
		var jsonPermit = JObject.Parse(formData);

		if (jsonPermit != null)
		{
			var permitCerts = (JArray)jsonPermit["general"]["certificates"];

			// Get current available workflow
			var workflow = _dbContext.Workflows
				.Include(e => e.WorkflowCompany)
				.FirstOrDefault(wf => wf.IsActive && wf.WorkflowCompany.Id == companyId && wf.HasCertificate == (permitCerts.Count > 0));

			if (workflow != null)
			{
				var locationId = (string)jsonPermit["general"]["location"]["id"];
				var loc = _dbContext.Sites.FirstOrDefault(f => f.Id.ToString().ToLower() == locationId);

				var permit = _dbContext.Permits.SingleOrDefault(f => f.Id == permitId);

				permit.PermitForm = form["PermitForm"];
				permit.PermitStatus = isDraft ? PermitStatusEnum.Draft : PermitStatusEnum.Pending;
				permit.Site = loc;
				permit.UpdatedWhen = DateTime.UtcNow.ToUniversalTime();
				permit.UpdatedBy = Guid.Parse(_currentUserService.GetCurrentUser().Id);

				_dbContext.Permits.Update(permit);
				await _dbContext.SaveChangesAsync();

				// Add audit log to database
				var currentUser = _currentUserService.GetCurrentUser();
				var fullName = $"{currentUser.FirstName} {currentUser.LastName}";
				var logMessage = $"Permit [PTW{permit.PermitNo}] has been updated by {fullName.Trim()} ({currentUser.Email}) ";
				logMessage += $"on {permit.CreatedWhen:dd/MM/yyyy hh:mm tt}.";

				await _logService.LogMessageAsync(LogTypeEnum.Information, "UPDATE_PERMIT", logMessage, currentUser);


				// Upload any attached documents
				if (form.Files.Count > 0)
				{
					var folderName = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", "permits", permitId.ToString().ToLower());

					if (!Directory.Exists(folderName))
					{
						Directory.CreateDirectory(folderName);
					}

					foreach (var item in form.Files)
					{
						var file = new FileInfo(item.FileName);
						var filePath = Path.Combine(folderName, file.Name);

						using (var fs = new FileStream(filePath, FileMode.Create))
						{
							await item.CopyToAsync(fs);
							fs.Flush();
						}
					}

					string[] filePaths = Directory.GetFiles(folderName);

					foreach (var filePath in filePaths)
					{
						var fi = new FileInfo(filePath);
						var currentAttachment = _dbContext.Attachments.FirstOrDefault(f => f.FileName == fi.Name);

						if (currentAttachment == null)
						{
							var attachment = new Attachment
							{
								FileName = fi.Name,
								ContentType = fi.Extension.Remove(0),
								Permit = permit,
								FileSize = (int)fi.Length,
							};

							_dbContext.Attachments.Add(attachment);
						}

					}
				}

				WorkflowStep workflowStep;

				if (isDraft)
				{
					workflowStep = _dbContext.WorkflowSteps
						.Include(wfs => wfs.Approvers)
						.Where(wfs => wfs.WorkflowStepWorkflow == workflow && wfs.Name == "Draft")
						.OrderBy(wfs => wfs.StepOrder)
						.FirstOrDefault();
				}
				else
				{
					workflowStep = _dbContext.WorkflowSteps
						.Include(wfs => wfs.Approvers)
						.Where(wfs => wfs.WorkflowStepWorkflow == workflow && wfs.Name != "Draft" && wfs.Name != "Completed")
						.OrderBy(wfs => wfs.StepOrder)
						.FirstOrDefault();
				}

				if (!isDraft && workflowStep.Approvers.Count > 0)
				{
					// Get email template
					var htmlTemplate = File.ReadAllText(Path.Combine(_webHostEnvironment.WebRootPath, "templates", "html", "wf-pending-approval.html"));
					var template = Template.Parse(htmlTemplate);

					// Generate email + internal notification for each approver
					var permitNoUpdate = string.Format("PTW{0:000000}", permit.RunningNumber);
					foreach (var approver in workflowStep.Approvers)
					{
						var approverName = $"{approver.FirstName} {approver.LastName}";
						var approvalLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/account/login?company={companyId}&entity=permits&id={permitId}&origin=email";
						var notifLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/{companyId}/permits/edit/{permitId}";

						var templateData = new
						{
							RecipientName = approverName,
							WorkflowName = workflow.Name,
							PermitNo = permitNoUpdate,
							DateSubmitted = GeneralHelper.GetDateInTimeZone(permit.CreatedWhen).ToString("dd/MM/yyyy hh:mm tt"),
							ApprovalLink = approvalLink,
						};

						var renderedHtml = template.Render(templateData);
						await _messageService.SendEmailAsync(new EmailInfo
						{
							Name = approverName,
							Email = approver.Email,
							Subject = $"Pending approval task for {permitNoUpdate}",
							Body = renderedHtml
						});

						_dbContext.Notifications.Add(new Notification
						{
							Title = "Approval Required",
							Message = $"You have a pending approval task for {permitNoUpdate}.",
							Url = notifLink,
							IsRead = false,
							IsArchived = false,
							NotificationUser = approver,
						});
						await _pushService.PushAsync(approver.Id, "Approval Required", $"You have a pending approval task for {permitNoUpdate}.");
					}
				}


				// Update workflow step in permit
				permit.PermitWorkflowStep = workflowStep;
				_dbContext.Permits.Update(permit);

				// Create a workflow history entry
				var wfh = new WorkflowHistory
				{
					Permit = permit,
					HistoryWorkflow = workflow,
					HistoryWorkflowStep = workflowStep,
					Status = isDraft ? WorkflowStatusEnum.Draft : WorkflowStatusEnum.Pending,
					Comments = isDraft ? "Permit's draft information has been successfully saved." : "Permit is now pending for approval.",
				};

				_dbContext.WorkflowHistories.Add(wfh);

				await _dbContext.SaveChangesAsync();
			}
			else
			{
				throw new Exception("Invalid workflow");
			}
		}
	}


	public async Task UpdateCertificateAsync()
	{
		var form = _httpContextAccessor.HttpContext.Request.Form;
		var permitId = Guid.Parse(form["PermitId"]);
		var formData = (string)form["PermitForm"];
		var companyId = Guid.Parse(form["CompanyId"]);

		// Create a new permit with posted values
		var jsonPermit = JObject.Parse(formData);

		if (jsonPermit != null)
		{
			var permit = _dbContext.Permits.SingleOrDefault(f => f.Id == permitId);

			permit.PermitForm = form["PermitForm"];
			permit.UpdatedWhen = DateTime.UtcNow.ToUniversalTime();
			permit.UpdatedBy = Guid.Parse(_currentUserService.GetCurrentUser().Id);

			_dbContext.Permits.Update(permit);
			await _dbContext.SaveChangesAsync();

			// Add audit log to database
			var currentUser = _currentUserService.GetCurrentUser();
			var fullName = $"{currentUser.FirstName} {currentUser.LastName}";
			var logMessage = $"Permit [PTW{permit.PermitNo}] has been updated by {fullName.Trim()} ({currentUser.Email}) ";
			logMessage += $"on {permit.CreatedWhen:dd/MM/yyyy hh:mm tt}.";

			await _logService.LogMessageAsync(LogTypeEnum.Information, "UPDATE_PERMIT", logMessage, currentUser);
		}
		else
		{
			throw new Exception("Invalid permit JSON");
		}
	}


	public async Task DeleteAsync(Permit entity)
	{
		var permitId = entity.Id;
		var permitNo = entity.PermitNo;

		// Delete any workflow history to this permit
		var wfh = _dbContext.WorkflowHistories
			.Include(f => f.Permit)
			.Where(f => f.Permit.Id == permitId);

		_dbContext.WorkflowHistories.RemoveRange(wfh);

		// Delete the permit
		//_dbContext.Permits.Remove(entity);
		await _dbContext.SoftDeleteAsync(entity, Guid.Parse(_currentUserService.GetCurrentUser().Id));

		// Delete file attachments
		var folderName = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", "permits", permitId.ToString().ToLower());

		if (Directory.Exists(folderName))
		{
			string[] filePaths = Directory.GetFiles(folderName);

			foreach (var filePath in filePaths)
			{
				FileInfo fi = new FileInfo(filePath);

				if (fi.Exists)
				{
					fi.Delete();
				}
			}

			Directory.Delete(folderName);
		}

		await _dbContext.SaveChangesAsync();

		// Add audit log to database
		var currentUser = _currentUserService.GetCurrentUser();
		var fullName = $"{currentUser.FirstName} {currentUser.LastName}";
		var logMessage = $"Permit [PTW{permitNo}] has been deleted by {fullName.Trim()} ({currentUser.Email}) ";
		logMessage += $"on {DateTime.UtcNow.ToUniversalTime():dd/MM/yyyy hh:mm tt}.";

		await _logService.LogMessageAsync(LogTypeEnum.Information, "DELETE_PERMIT", logMessage, currentUser);
	}


	public async Task SetPermitStatusAsync()
	{
		var request = _httpContextAccessor.HttpContext.Request.Form;
		var currentUser = _currentUserService.GetCurrentUser();
		var fullName = $"{currentUser.FirstName} {currentUser.LastName}";
		var logMessage = string.Empty;

		var permit = _dbContext.Permits
			.Include(f => f.PermitWorkflowStep)
			.ThenInclude(wfs => wfs.WorkflowStepWorkflow)
			.SingleOrDefault(f => f.Id == Guid.Parse(request["PermitId"]));

		if (permit == null)
		{
			throw new Exception($"Unable to find permit with permit ID: {request["PermitId"]}");
		}

		var workflowStep = permit.PermitWorkflowStep;
		var workflow = workflowStep.WorkflowStepWorkflow;

		var workflowSteps = _dbContext.WorkflowSteps
			.Where(f => f.WorkflowStepWorkflow.Id == workflow.Id)
			.OrderBy(f => f.StepOrder);

		WorkflowStep nextWfs = null;
		WorkflowStep completedWfs = null;
		bool checkForNext = false;
		bool workflowCompleted = false;

		foreach (var step in workflowSteps)
		{
			if (checkForNext)
			{
				if (step.StepOrder == 100)
				{
					workflowCompleted = true;
					completedWfs = step;
				}
				else
				{
					nextWfs = step;
					break;
				}
			}

			checkForNext = (step.Id == workflowStep.Id);
		}

		if (request["Mode"] == "approve")
		{
			var dateApproved = DateTime.UtcNow.ToUniversalTime();

			// If approval step is not completed, move to the next step
			if (!workflowCompleted)
			{
				permit.PermitWorkflowStep = nextWfs;
				_dbContext.Permits.Update(permit);

				var wfh = new WorkflowHistory
				{
					Permit = permit,
					Comments = !string.IsNullOrEmpty(request["Comments"]) ? request["Comments"] : $"{fullName.Trim()} has approved this permit.",
					HistoryWorkflow = workflow,
					HistoryWorkflowStep = workflowStep,
					Status = WorkflowStatusEnum.Approved,
					ApprovedWhen = dateApproved,
				};
				_dbContext.WorkflowHistories.Add(wfh);

				// Notify next-step approvers
				if (nextWfs != null)
				{
					var nextApprovers = _dbContext.WorkflowSteps
						.Include(s => s.Approvers)
						.FirstOrDefault(s => s.Id == nextWfs.Id)?.Approvers ?? new();
					var permitNoNext = string.Format("PTW{0:000000}", permit.RunningNumber);
					foreach (var nextApprover in nextApprovers)
					{
						var nextLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/{permit.Company?.Id}/permits/{permit.Id}/edit";
						_dbContext.Notifications.Add(new Notification
						{
							Title = "Approval Required",
							Message = $"You have a pending approval task for {permitNoNext}.",
							Url = nextLink,
							IsRead = false,
							IsArchived = false,
							NotificationUser = nextApprover,
						});
						await _pushService.PushAsync(nextApprover.Id, "Approval Required", $"You have a pending approval task for {permitNoNext}.");
					}
				}
			}
			else
			{
				permit.ApprovedDateTime = dateApproved;
				permit.ResumedDateTime = null;
				permit.RejectedDateTime = null;

				permit.PermitWorkflowStep = completedWfs;
				permit.PermitStatus = PermitStatusEnum.Approved;
				permit.UpdatedBy = Guid.Parse(currentUser.Id);
				permit.UpdatedWhen = DateTime.UtcNow.ToUniversalTime();

				_dbContext.Permits.Update(permit);

				var wfh = new WorkflowHistory
				{
					Permit = permit,
					Comments = !string.IsNullOrEmpty(request["Comments"]) ? request["Comments"] : $"{fullName.Trim()} has approved this permit.",
					HistoryWorkflow = workflow,
					HistoryWorkflowStep = workflowStep,
					Status = WorkflowStatusEnum.Approved,
					ApprovedWhen = dateApproved,
				};
				_dbContext.WorkflowHistories.Add(wfh);

				// Notify permit creator: fully approved
				var creator = _dbContext.Users.FirstOrDefault(u => u.Id == permit.CreatedBy.ToString());
				if (creator != null)
				{
					var approvedPermitNo = string.Format("PTW{0:000000}", permit.RunningNumber);
					var approvedLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/{permit.Company?.Id}/permits/{permit.Id}/edit";
					_dbContext.Notifications.Add(new Notification
					{
						Title = "Permit Approved",
						Message = $"{approvedPermitNo} has been fully approved.",
						Url = approvedLink,
						IsRead = false,
						IsArchived = false,
						NotificationUser = creator,
					});
					await _pushService.PushAsync(creator.Id, "Permit Approved", $"{approvedPermitNo} has been fully approved.");
				}
			}

			logMessage = $"Permit [PTW{permit.PermitNo}] has been approved by {fullName.Trim()} ({currentUser.Email}) on {dateApproved:dd/MM/yyy hh:mm tt}.";
		}
		else
		{
			var dateRejected = DateTime.UtcNow.ToUniversalTime();

			permit.PermitStatus = PermitStatusEnum.Rejected;

			permit.ApprovedDateTime = null;
			permit.ResumedDateTime = null;
			permit.RejectedDateTime = dateRejected;
			permit.UpdatedBy = Guid.Parse(currentUser.Id);
			permit.UpdatedWhen = DateTime.UtcNow.ToUniversalTime();

			_dbContext.Permits.Update(permit);

			var wfh = new WorkflowHistory
			{
				Permit = permit,
				Comments = !string.IsNullOrEmpty(request["Comments"]) ? request["Comments"] : $"{fullName.Trim()} has rejected this permit.",
				HistoryWorkflow = workflow,
				HistoryWorkflowStep = workflowStep,
				Status = WorkflowStatusEnum.Rejected,
				RejectedWhen = dateRejected,
			};
			_dbContext.WorkflowHistories.Add(wfh);

				// Notify permit creator: rejected
				var rejCreator = _dbContext.Users.FirstOrDefault(u => u.Id == permit.CreatedBy.ToString());
				if (rejCreator != null)
				{
					var rejPermitNo = string.Format("PTW{0:000000}", permit.RunningNumber);
					var rejLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}/{permit.Company?.Id}/permits/{permit.Id}/edit";
					_dbContext.Notifications.Add(new Notification
					{
						Title = "Permit Rejected",
						Message = $"{rejPermitNo} has been rejected.",
						Url = rejLink,
						IsRead = false,
						IsArchived = false,
						NotificationUser = rejCreator,
					});
					await _pushService.PushAsync(rejCreator.Id, "Permit Rejected", $"{rejPermitNo} has been rejected.");
				}

			logMessage = $"Permit [PTW{permit.PermitNo}] has been rejected by {fullName.Trim()} ({currentUser.Email}) on {dateRejected:dd/MM/yyy hh:mm tt}.";
		}

		await _dbContext.SaveChangesAsync();

		await _logService.LogMessageAsync(LogTypeEnum.Information, "PERMIT_APPROVAL", logMessage, currentUser);

		//throw new NotImplementedException();
	}


	public async Task ClosePermitAsync()
	{
		var form = _httpContextAccessor.HttpContext.Request.Form;
		var currentUser = _currentUserService.GetCurrentUser();
		var permitId = Guid.Parse(form["PermitId"]);
		var notes = (string)form["Notes"];

		var permit = _dbContext.Permits
			.Include(e => e.PermitWorkflow)
			.Include(e => e.PermitWorkflowStep)
			.Single(e => e.Id == permitId);

		if (permit == null)
		{
			throw new Exception("Permit not found");
		}

		permit.Remarks = notes;
		permit.PermitStatus = PermitStatusEnum.Closed;
		permit.UpdatedBy = Guid.Parse(currentUser.Id);
		permit.UpdatedWhen = DateTime.UtcNow.ToUniversalTime();

		// Create a workflow history entry
		var wfh = new WorkflowHistory
		{
			Permit = permit,
			HistoryWorkflow = permit.PermitWorkflow,
			HistoryWorkflowStep = permit.PermitWorkflowStep,
			Status = WorkflowStatusEnum.Closed,
			Comments = !string.IsNullOrEmpty(notes) ? notes : "Permit has been marked as closed.",
		};

		_dbContext.WorkflowHistories.Add(wfh);
		_dbContext.Permits.Update(permit);

		await _dbContext.SaveChangesAsync();


		// Add audit log to database
		var fullName = $"{currentUser.FirstName} {currentUser.LastName}";
		var logMessage = $"Permit [PTW{permit.PermitNo}] has been marked as closed by {fullName.Trim()} on {DateTime.UtcNow.ToUniversalTime():dd/MM/yyyy hh:mm tt}.";

		await _logService.LogMessageAsync(LogTypeEnum.Information, "PERMIT_CLOSED", logMessage, currentUser);
	}


	public Permit GetById(Guid entityId)
	{
		return _dbContext.Permits.FirstOrDefault(x => x.Id == entityId);
	}


	public IEnumerable<Permit> GetAll()
	{
		return _dbContext.Permits;
	}


	public IEnumerable<Permit> GetPermitsBySite(Site site)
	{
		var permits = _dbContext.Permits
			.Include(x => x.Site)
			.Where(x => x.Site == site);

		return permits;
	}


	public IEnumerable<Permit> GetPermitsBySite(Guid id)
	{
		var permits = _dbContext.Permits
			.Include(x => x.Site)
			.Where(x => x.Site!.Id == id);

		return permits;
	}


	public async Task<byte[]> GetPdfBytesAsync(IFormCollection form)
	{
		byte[] pdfResult = null;

		var permit = _dbContext.Permits
			.Include(e => e.Company)
			.Include(e => e.WorkflowHistories)
			.Include(e => e.PermitWorkflowStep)
			.ThenInclude(e => e.Approvers)
			.Where(e => e.Id == Guid.Parse(form["Id"]))
			.FirstOrDefault();

		if (permit == null)
		{
			return null;
		}

		var jsonObject = JObject.Parse(permit.PermitForm!);
		var jo = JsonConvert.DeserializeObject(permit.PermitForm);

		var mainFormFile = Path.Combine(_webHostEnvironment.WebRootPath, "templates", "html", "permit-main.html");
		var html = File.ReadAllText(mainFormFile, Encoding.UTF8);

		//var ptwCertificates = new[] { "hotwork", "confinedspace", "radiation", "excavation", "isolation", "methodStatement", "liftingHoisting", "override" };

		List<dynamic> mainFormCerts = new();
		List<dynamic> attachedCerts = new();

		JArray certificates = (JArray)jsonObject["general"]["certificates"];
		JArray opInfoActivities = (JArray)jsonObject["operation"]["activities"];
		JArray opInfoLocations = (JArray)jsonObject["operation"]["activityLocations"];
		JArray opInfoTools = (JArray)jsonObject["operation"]["activityTools"];
		JArray opInfoProducts = (JArray)jsonObject["operation"]["activityProducts"];

		dynamic formA = null;
		dynamic formB = null;
		dynamic formC = null;
		dynamic formD = null;
		dynamic formE = null;
		dynamic formF = null;
		dynamic formG = null;
		dynamic formH = null;

		/**
		 * PTW certificates
		 */
		foreach (var item in _ptwSettings.Certificates)
		{
			var cert = certificates.SelectToken($"$.[?(@.name=='{item.Name}')]");

			attachedCerts.Add(new
			{
				item.Name,
				item.Title,
				item.Description,
				Data = cert,
			});

			mainFormCerts.Add(new
			{
				item.Name,
				item.Title,
				item.Description,
				Yes = GetCheckBoxImage(cert != null),
				No = GetCheckBoxImage(cert == null),
			});

			if (cert != null)
			{
				html = html.Replace($"__FORM_{item.Description}__", GetCertificateHtmlTemplate(_webHostEnvironment.WebRootPath, item.Name));

				switch (item.Name)
				{
					case "hotwork":
						formA = new
						{
							EmergencyContact = TemplateConvertString(cert["emergencyContact"]),
							WorkChecklist = new
							{
								List1 = GetCheckBoxImage((bool)cert["workChecklist"][0]["value"]),
								List2 = GetCheckBoxImage((bool)cert["workChecklist"][1]["value"]),
								List3 = GetCheckBoxImage((bool)cert["workChecklist"][2]["value"]),
								List4 = GetCheckBoxImage((bool)cert["workChecklist"][3]["value"]),
								List5 = GetCheckBoxImage((bool)cert["workChecklist"][4]["value"]),
								List6 = GetCheckBoxImage((bool)cert["workChecklist"][5]["value"]),
								List7 = GetCheckBoxImage((bool)cert["workChecklist"][6]["value"]),
							},
							IgnitionSources = new
							{
								Source1 = GetCheckBoxImage((bool)cert["ignitionSources"][0]["value"]),
								Source2 = GetCheckBoxImage((bool)cert["ignitionSources"][1]["value"]),
								Source3 = GetCheckBoxImage((bool)cert["ignitionSources"][2]["value"]),
								Source4 = GetCheckBoxImage((bool)cert["ignitionSources"][3]["value"]),
								Source5 = new
								{
									Checkbox = GetCheckBoxImage((bool)cert["ignitionSources"][4]["value"]),
									Value = TemplateConvertString(cert["ignitionSources"][4]["other"], false, true),
								},
							},
							GasTestPerformedBy = new
							{
								Id = cert["gasTestPerformedBy"]["id"],
								Name = TemplateConvertString(cert["gasTestPerformedBy"]["name"]),
							},
							LowExplosionLevel = new
							{
								InitialTestDateTime = TemplateConvertString(cert["lowExplosionLevel"]["initialTestDateTime"]),
								InitialReading = TemplateConvertString(cert["lowExplosionLevel"]["initialReading"]),
								RequiredFrequency = TemplateConvertString(cert["lowExplosionLevel"]["requiredFrequency"]),
								Readings = cert["lowExplosionLevel"]["readings"],
							},
							AtmosTestReadings = cert["atmosTestReadings"],
							PermitValidity = new
							{
								From = TemplateConvertString(cert["permitValidity"]["from"]),
								To = TemplateConvertString(cert["permitValidity"]["to"]),
							},
							PermitWithdrawn = new
							{
								YesNo = TemplateConvertString(cert["permitWithdrawn"]["status"], true),
								Reason = TemplateConvertString(cert["permitWithdrawn"]["reason"]),
							},
						};

						break;

					case "confinedspace":
						formB = new
						{
							Attendants = TemplateConvertString(cert["attendants"]),
							WorkDescription = TemplateConvertString(cert["workDescription"]),
							GasTest = new
							{
								PerformedBy = TemplateConvertString(cert["gasTest"]["performedBy"]),
								InitialTestWhen = TemplateConvertString(cert["gasTest"]["initialTestWhen"]),
								InitialLEL = TemplateConvertString(cert["gasTest"]["initialLEL"]),
								InitialO2 = TemplateConvertString(cert["gasTest"]["initialO2"]),
								Others = new
								{
									Toxic1 = TemplateConvertString(cert["gasTest"]["others"][0]),
									Toxic2 = TemplateConvertString(cert["gasTest"]["others"][1]),
								},
								RequiredFrequency = TemplateConvertString(cert["gasTest"]["requiredFrequency"]),
								Readings = cert["gasTest"]["readings"],
							},
							PotentialHazards = new
							{
								HydrogenSulphide = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["hydrogenSulphide"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["hydrogenSulphide"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["hydrogenSulphide"]["na"]),
								},
								OxygenDeficiency = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["oxygenDeficiency"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["oxygenDeficiency"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["oxygenDeficiency"]["na"]),
								},
								FlammableGas = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["flammableGas"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["flammableGas"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["flammableGas"]["na"]),
								},
								FlammableSolids = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["flammableSolids"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["flammableSolids"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["flammableSolids"]["na"]),
								},
								Electrical = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["electrical"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["electrical"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["electrical"]["na"]),
								},
								IonizingRadiation = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["ionizingRadiation"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["ionizingRadiation"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["ionizingRadiation"]["na"]),
								},
								BelowGroundLevel = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["belowGroundLevel"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["belowGroundLevel"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["belowGroundLevel"]["na"]),
								},
								EnvironmentalConditions = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["environmentalConditions"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["environmentalConditions"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["environmentalConditions"]["na"]),
								},
								EngulfmentLiquidsOrSolids = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["engulfmentLiquidsOrSolids"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["engulfmentLiquidsOrSolids"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["engulfmentLiquidsOrSolids"]["na"]),
								},
								Temperature = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["temperature"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["temperature"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["temperature"]["na"]),
								},
								IgnitionSources = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["ignitionSources"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["ignitionSources"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["ignitionSources"]["na"]),
								},
								Other1 = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["other1"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["other1"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["other1"]["na"]),
									Value = TemplateConvertString(cert["potentialHazards"]["other1"]["value"], false, true),
								},
								Other2 = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["other2"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["other2"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["other2"]["na"]),
									Value = TemplateConvertString(cert["potentialHazards"]["other2"]["value"], false, true),
								},
								Other3 = new
								{
									Yes = GetCheckBoxImage((bool)cert["potentialHazards"]["other3"]["yes"]),
									No = GetCheckBoxImage((bool)cert["potentialHazards"]["other3"]["no"]),
									Na = GetCheckBoxImage((bool)cert["potentialHazards"]["other3"]["na"]),
									Value = TemplateConvertString(cert["potentialHazards"]["other3"]["value"], false, true),
								},
							},
							CommunicationRequirements = new
							{
								Radio = GetCheckBoxImage((bool)cert["communicationRequirements"]["radio"]),
								Voice = GetCheckBoxImage((bool)cert["communicationRequirements"]["voice"]),
								Visual = GetCheckBoxImage((bool)cert["communicationRequirements"]["visual"]),
								Other = new
								{
									Checkbox = GetCheckBoxImage((bool)cert["communicationRequirements"]["other"]["state"]),
									Value = TemplateConvertString(cert["communicationRequirements"]["other"]["value"], false, true),
								},
							},
							PpeEquipment = new
							{
								SelfContainedBreathingApparatus = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["selfContainedBreathingApparatus"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["selfContainedBreathingApparatus"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["selfContainedBreathingApparatus"]["na"]),
								},
								SuppliedAirBreathingApparatus = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["suppliedAirBreathingApparatus"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["suppliedAirBreathingApparatus"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["suppliedAirBreathingApparatus"]["na"]),
								},
								GroundFaultInterrupters = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["groundFaultInterrupters"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["groundFaultInterrupters"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["groundFaultInterrupters"]["na"]),
								},
								Ropes = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["ropes"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["ropes"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["ropes"]["na"]),
								},
								FullBodyHarness = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["fullBodyHarness"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["fullBodyHarness"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["fullBodyHarness"]["na"]),
								},
								ProtectionProvided = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["protectionProvided"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["protectionProvided"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["protectionProvided"]["na"]),
								},
								Barriers = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["barriers"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["barriers"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["barriers"]["na"]),
								},
								VentilationEquipment = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["ventilationEquipment"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["ventilationEquipment"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["ventilationEquipment"]["na"]),
								},
								CalibratedGasTestingEquipment = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["calibratedGasTestingEquipment"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["calibratedGasTestingEquipment"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["calibratedGasTestingEquipment"]["na"]),
								},
								PortableLighting = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["portableLighting"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["portableLighting"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["portableLighting"]["na"]),
								},
								Other = new
								{
									Yes = GetCheckBoxImage((bool)cert["ppeEquipment"]["other"]["yes"]),
									No = GetCheckBoxImage((bool)cert["ppeEquipment"]["other"]["no"]),
									Na = GetCheckBoxImage((bool)cert["ppeEquipment"]["other"]["na"]),
									Value = TemplateConvertString(cert["ppeEquipment"]["other"]["value"], false, true),
								},
							},
							RescueEquipment = new
							{
								RescuePackage = new
								{
									Yes = GetCheckBoxImage((bool)cert["rescueEquipment"]["rescuePackage"]["yes"]),
									No = GetCheckBoxImage((bool)cert["rescueEquipment"]["rescuePackage"]["no"]),
									Na = GetCheckBoxImage((bool)cert["rescueEquipment"]["rescuePackage"]["na"]),
									Horizontal = new
									{
										Yes = GetCheckBoxImage((bool)cert["rescueEquipment"]["rescuePackage"]["horizontal"]["yes"]),
										No = GetCheckBoxImage((bool)cert["rescueEquipment"]["rescuePackage"]["horizontal"]["no"]),
										Na = GetCheckBoxImage((bool)cert["rescueEquipment"]["rescuePackage"]["horizontal"]["na"]),
									},
									Vertical = new
									{
										Yes = GetCheckBoxImage((bool)cert["rescueEquipment"]["rescuePackage"]["vertical"]["yes"]),
										No = GetCheckBoxImage((bool)cert["rescueEquipment"]["rescuePackage"]["vertical"]["no"]),
										Na = GetCheckBoxImage((bool)cert["rescueEquipment"]["rescuePackage"]["vertical"]["na"]),
									},
								},
								PortableBreathingAirSystems = new
								{
									Yes = GetCheckBoxImage((bool)cert["rescueEquipment"]["portableBreathingAirSystems"]["yes"]),
									No = GetCheckBoxImage((bool)cert["rescueEquipment"]["portableBreathingAirSystems"]["no"]),
									Na = GetCheckBoxImage((bool)cert["rescueEquipment"]["portableBreathingAirSystems"]["na"]),
								},
								SafeExit = new
								{
									Yes = GetCheckBoxImage((bool)cert["rescueEquipment"]["safeExit"]["yes"]),
									No = GetCheckBoxImage((bool)cert["rescueEquipment"]["safeExit"]["no"]),
									Na = GetCheckBoxImage((bool)cert["rescueEquipment"]["safeExit"]["na"]),
								},
								DefibrillatorAvailable = new
								{
									Yes = GetCheckBoxImage((bool)cert["rescueEquipment"]["defibrillatorAvailable"]["yes"]),
									No = GetCheckBoxImage((bool)cert["rescueEquipment"]["defibrillatorAvailable"]["no"]),
									Na = GetCheckBoxImage((bool)cert["rescueEquipment"]["defibrillatorAvailable"]["na"]),
								},
								FireExtinguishersAvailable = new
								{
									Yes = GetCheckBoxImage((bool)cert["rescueEquipment"]["fireExtinguishersAvailable"]["yes"]),
									No = GetCheckBoxImage((bool)cert["rescueEquipment"]["fireExtinguishersAvailable"]["no"]),
									Na = GetCheckBoxImage((bool)cert["rescueEquipment"]["fireExtinguishersAvailable"]["na"]),
								},
								Other = new
								{
									Yes = GetCheckBoxImage((bool)cert["rescueEquipment"]["other"]["yes"]),
									No = GetCheckBoxImage((bool)cert["rescueEquipment"]["other"]["no"]),
									Na = GetCheckBoxImage((bool)cert["rescueEquipment"]["other"]["na"]),
									Value = TemplateConvertString(cert["rescueEquipment"]["other"]["value"], false, true),
								},
							},
							PermitValidity = new
							{
								From = TemplateConvertString(cert["permitValidity"]["from"]),
								To = TemplateConvertString(cert["permitValidity"]["to"]),
							},
							PermitWithdrawn = new
							{
								YesNo = TemplateConvertString(cert["permitWithdrawn"]["status"], true),
								Reason = TemplateConvertString(cert["permitWithdrawn"]["reason"]),
							},
							EntryRecords = cert["entryRecords"],
						};

						break;

					case "radiation":
						formC = new
						{
							RadioactiveMaterialUsed = TemplateConvertString(cert["radioactiveMaterialUsed"]),
							MaterialActivity = TemplateConvertString(cert["materialActivity"]),
							ContainedWithin = TemplateConvertString(cert["containedWithin"]),
							XRay = new
							{
								Checkbox = GetCheckBoxImage((bool)cert["xray"]["state"]),
								Manufacturer = TemplateConvertString(cert["xray"]["manufacturer"]),
								MaxTubeVoltage = TemplateConvertString(cert["xray"]["maxTubeVoltage"]),
								MaxTubeCurrent = TemplateConvertString(cert["xray"]["maxTubeCurrent"]),
							},
							Other = new
							{
								Checkbox = GetCheckBoxImage((bool)cert["other"]["state"]),
								Value = TemplateConvertString(cert["other"]["value"], false, true),
							},
							AdditionalInfo = TemplateConvertString(cert["additionalInfo"]),
							RadiationEquipmentUsed = new
							{
								From = TemplateConvertString(cert["radiationEquipmentUsed"]["from"]),
								To = TemplateConvertString(cert["radiationEquipmentUsed"]["to"]),
							},
							MeasuresPreventExposure = TemplateConvertString(cert["measuresPreventExposure"]),
							AdditionalControls = TemplateConvertString(cert["additionalControls"]),
							PermitValidity = new
							{
								From = TemplateConvertString(cert["permitValidity"]["from"]),
								To = TemplateConvertString(cert["permitValidity"]["to"]),
							},
							PermitWithdrawn = new
							{
								YesNo = TemplateConvertString(cert["permitWithdrawn"]["status"], true),
								Reason = TemplateConvertString(cert["permitWithdrawn"]["reason"]),
							},
						};

						break;

					case "excavation":
						formD = new
						{
							ExcavationDepth = TemplateConvertString(cert["excavationDepth"]),
							ControlsUsed = new
							{
								Sloping = GetCheckBoxImage((bool)cert["controlsUsed"]["sloping"]),
								Shoring = GetCheckBoxImage((bool)cert["controlsUsed"]["shoring"]),
								Shielding = GetCheckBoxImage((bool)cert["controlsUsed"]["shielding"]),
							},
							DesignatedAuthorizedPerson = TemplateConvertString(cert["designatedAuthorizedPerson"]),
							AdditionalInfo = TemplateConvertString(cert["additionalInfo"]),
							AdditionalSpaceSketch = TemplateConvertString(cert["additionalSpaceSketch"]),
							EmergencyContact = TemplateConvertString(cert["emergencyContact"]),
							PermitValidity = new
							{
								From = TemplateConvertString(cert["permitValidity"]["from"]),
								To = TemplateConvertString(cert["permitValidity"]["to"]),
							},
							PermitWithdrawn = new
							{
								YesNo = TemplateConvertString(cert["permitWithdrawn"]["status"], true),
								Reason = TemplateConvertString(cert["permitWithdrawn"]["reason"]),
							},
						};

						break;

					case "isolation":
						JArray tmp1 = (JArray)cert["authorizedIndividuals"];
						JArray tmp2 = (JArray)cert["isolationPoints"];

						List<dynamic> list1 = new List<dynamic>();
						List<dynamic> list2 = new List<dynamic>();

						if (tmp1 != null)
						{
							foreach (var n in tmp1)
							{
								list1.Add(new
								{
									Date = TemplateConvertString(n["date"].Value<JValue>()),
									Name = TemplateConvertString(n["name"].Value<JValue>()),
									Company = TemplateConvertString(n["company"].Value<JValue>()),
								});
							}
						}

						if (tmp2 != null)
						{
							foreach (var n in tmp2)
							{
								list2.Add(new
								{
									IsolationPoint = TemplateConvertString(n["isolationPoint"].Value<JValue>()),
									TagInstalled = GetCheckBoxImage((bool)n["tagInstalled"].Value<JValue>()),
									LockInstalled = GetCheckBoxImage((bool)n["lockInstalled"].Value<JValue>()),
									BlindInstalled = GetCheckBoxImage((bool)n["blindInstalled"].Value<JValue>()),
									NormalOperatePos = TemplateConvertString(n["normalOperatePos"].Value<JValue>()),
									DateInstalled = TemplateConvertString(n["dateInstalled"].Value<JValue>()),
									DateRemoved = TemplateConvertString(n["dateRemoved"].Value<JValue>()),
								});
							}
						}

						JToken permitValidityFrom = null;
						JToken permitValidityTo = null;

						if (cert["permitValidity"] != null)
						{
							permitValidityFrom = cert["permitValidity"]["from"];
							permitValidityTo = cert["permitValidity"]["to"];
						}

						formE = new
						{
							EquipmentUsed = cert["equipmentUsed"],
							LockOutTagOutType = new
							{
								Individual = GetCheckBoxImage((bool)cert["lockOutTagOutType"]["individual"]),
								Group = GetCheckBoxImage((bool)cert["lockOutTagOutType"]["group"]),
							},
							IsolatedEnergyType = new
							{
								Electrical = GetCheckBoxImage((bool)cert["isolatedEnergyType"]["electrical"]),
								Steam = GetCheckBoxImage((bool)cert["isolatedEnergyType"]["steam"]),
								Hydraulic = GetCheckBoxImage((bool)cert["isolatedEnergyType"]["hydraulic"]),
								Mechanical = GetCheckBoxImage((bool)cert["isolatedEnergyType"]["mechanical"]),
								Pneumatic = GetCheckBoxImage((bool)cert["isolatedEnergyType"]["pneumatic"]),
								Other = new
								{
									Checkbox = GetCheckBoxImage((bool)cert["isolatedEnergyType"]["other"]["state"]),
									Value = TemplateConvertString(cert["isolatedEnergyType"]["other"]["value"]),
								}
							},
							IsolationMethod = new
							{
								LockTag = GetCheckBoxImage((bool)cert["isolationMethod"]["lockTag"]),
								TagOnly = GetCheckBoxImage((bool)cert["isolationMethod"]["tagOnly"]),
								Blinds = GetCheckBoxImage((bool)cert["isolationMethod"]["blinds"]),
								DoubleBlockBleed = GetCheckBoxImage((bool)cert["isolationMethod"]["doubleBlockBleed"]),
								Other = new
								{
									Checkbox = GetCheckBoxImage((bool)cert["isolationMethod"]["other"]["state"]),
									Value = TemplateConvertString(cert["isolationMethod"]["other"]["value"]),
								}
							},
							AuthorizedIndividuals = list1,
							IsolationPoints = list2,
							PermitValidity = new
							{
								From = TemplateConvertString(permitValidityFrom),
								To = TemplateConvertString(permitValidityTo),
							},
						};

						break;

					case "methodStatement":
						formF = new
						{
							WorksDescription = new
							{
								Desc1 = TemplateConvertString(cert["worksDescription"]["1"]),
								Desc2 = TemplateConvertString(cert["worksDescription"]["2"]),
								Desc3 = TemplateConvertString(cert["worksDescription"]["3"]),
								Desc4 = TemplateConvertString(cert["worksDescription"]["4"]),
								Desc5 = TemplateConvertString(cert["worksDescription"]["5"]),
								Desc6 = TemplateConvertString(cert["worksDescription"]["6"]),
								Desc7 = TemplateConvertString(cert["worksDescription"]["7"]),
								Desc8 = TemplateConvertString(cert["worksDescription"]["8"]),
							},
							ResponsiblePersonNames = new
							{
								Person1 = TemplateConvertString(cert["responsiblePersonNames"]["1"]),
								Person2 = TemplateConvertString(cert["responsiblePersonNames"]["2"]),
								Person3 = TemplateConvertString(cert["responsiblePersonNames"]["3"]),
								Person4 = TemplateConvertString(cert["responsiblePersonNames"]["4"]),
								Person5 = TemplateConvertString(cert["responsiblePersonNames"]["5"]),
							},
							PlantEquipment = new
							{
								Equip1 = TemplateConvertString(cert["plantEquipment"]["1"]),
								Equip2 = TemplateConvertString(cert["plantEquipment"]["2"]),
								Equip3 = TemplateConvertString(cert["plantEquipment"]["3"]),
								Equip4 = TemplateConvertString(cert["plantEquipment"]["4"]),
								Equip5 = TemplateConvertString(cert["plantEquipment"]["5"]),
								Equip6 = TemplateConvertString(cert["plantEquipment"]["6"]),
								Equip7 = TemplateConvertString(cert["plantEquipment"]["7"]),
								Equip8 = TemplateConvertString(cert["plantEquipment"]["8"]),
								Equip9 = TemplateConvertString(cert["plantEquipment"]["9"]),
							},
							CommunicationsWorkforce = new
							{
								Comm1 = TemplateConvertString(cert["communicationsWorkforce"]["1"]),
								Comm2 = TemplateConvertString(cert["communicationsWorkforce"]["2"]),
								Comm3 = TemplateConvertString(cert["communicationsWorkforce"]["3"]),
								Comm4 = TemplateConvertString(cert["communicationsWorkforce"]["4"]),
							},
							WorkToBeCompleted = new
							{
								Work1 = TemplateConvertString(cert["workToBeCompleted"]["1"]),
								Work2 = TemplateConvertString(cert["workToBeCompleted"]["2"]),
								Work3 = TemplateConvertString(cert["workToBeCompleted"]["3"]),
							},
						};

						break;

					case "liftingHoisting":
						formG = new
						{
							Location = TemplateConvertString(cert["location"]),
							LiftingPlanNo = TemplateConvertString(cert["liftingPlanNo"]),
							TypeOfLift = new
							{
								Level1 = GetCheckBoxImage((bool)cert["typeOfLift"]["level1"]),
								Level2 = GetCheckBoxImage((bool)cert["typeOfLift"]["level2"]),
							},
							SafeChecks = new
							{
								Check1 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["1"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["1"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["1"]["na"]),
								},
								Check2 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["2"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["2"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["2"]["na"]),
								},
								Check3 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["3"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["3"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["3"]["na"]),
								},
								Check4 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["4"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["4"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["4"]["na"]),
								},
								Check5 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["5"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["5"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["5"]["na"]),
								},
								Check6 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["6"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["6"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["6"]["na"]),
								},
								Check7 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["7"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["7"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["7"]["na"]),
								},
								Check8 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["8"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["8"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["8"]["na"]),
								},
								Check9 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["9"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["9"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["9"]["na"]),
								},
								Check10 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["10"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["10"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["10"]["na"]),
								},
								Check11 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["11"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["11"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["11"]["na"]),
								},
								Check12 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["12"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["12"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["12"]["na"]),
								},
								Check13 = new
								{
									Yes = GetCheckBoxImage((bool)cert["safeChecks"]["13"]["yes"]),
									No = GetCheckBoxImage((bool)cert["safeChecks"]["13"]["no"]),
									Na = GetCheckBoxImage((bool)cert["safeChecks"]["13"]["na"]),
								},
							},
							PermitValidity = new
							{
								From = TemplateConvertString(cert["permitValidity"]["from"]),
								To = TemplateConvertString(cert["permitValidity"]["to"]),
								DateSignedLPI = TemplateConvertString(cert["permitValidity"]["dateSignedLPI"]),
								SignedEngineering = GetCheckBoxImage((bool)cert["permitValidity"]["signedEngineering"]),
								DateSignedEngineering = TemplateConvertString(cert["permitValidity"]["dateSignedEngineering"]),
								SignedPH = GetCheckBoxImage((bool)cert["permitValidity"]["signedPH"]),
								DateSignedPH = TemplateConvertString(cert["permitValidity"]["dateSignedPH"]),
							},
							PermitWithdrawn = new
							{
								YesNo = TemplateConvertString(cert["permitWithdrawn"]["status"], true),
								Reason = TemplateConvertString(cert["permitWithdrawn"]["reason"]),
							},
						};

						break;

					case "override":
						formH = new
						{
							General = new
							{
								Item1 = TemplateConvertString(cert["general"]["1"]),
								Item2 = TemplateConvertString(cert["general"]["2"]),
								Item3 = TemplateConvertString(cert["general"]["3"]),
								Item4 = TemplateConvertString(cert["general"]["4"]),
								Item5 = TemplateConvertString(cert["general"]["5"]),
								Item6 = TemplateConvertString(cert["general"]["6"]),
							},
							PlannedDuration = new
							{
								From = TemplateConvertString(cert["plannedDuration"]["from"]),
								To = TemplateConvertString(cert["plannedDuration"]["to"]),
							},
							PermitValidity = new
							{
								From = TemplateConvertString(cert["permitValidity"]["from"]),
								To = TemplateConvertString(cert["permitValidity"]["to"]),
								SignedLPI = GetCheckBoxImage((bool)cert["permitValidity"]["signedLPI"]),
								DateSignedLPI = TemplateConvertString(cert["permitValidity"]["dateSignedLPI"]),
								SignedPH = GetCheckBoxImage((bool)cert["permitValidity"]["signedPH"]),
								DateSignedPH = TemplateConvertString(cert["permitValidity"]["dateSignedPH"]),
							},
						};

						break;
				}
			}
			else
			{
				html = html.Replace($"__FORM_{item.Description}__", "");
			}
		}


		/**
		 * Operation Info > Activities
		 */
		List<dynamic> oiActivities = new();

		foreach (var item in opInfoActivities)
		{
			oiActivities.Add(new
			{
				Checkbox = GetCheckBoxImage((bool)item["status"]),
				Value = item["value"],
			});
		}


		/**
		 * Operation Info > Location
		 */
		List<dynamic> oiLocations = new();

		foreach (var item in opInfoLocations)
		{
			oiLocations.Add(new
			{
				Checkbox = GetCheckBoxImage((bool)item["status"]),
				Value = item["value"],
			});
		}


		/**
		 * Operation Info > Tools
		 */
		List<dynamic> oiTools = new();

		foreach (var item in opInfoTools)
		{
			oiTools.Add(new
			{
				Checkbox = GetCheckBoxImage((bool)item["status"]),
				Value = item["value"],
			});
		}


		/**
		 * Operation Info > Products
		 */
		List<dynamic> oiProducts = new();

		foreach (var item in opInfoProducts)
		{
			oiProducts.Add(new
			{
				Id = item["id"],
				Checkbox = GetCheckBoxImage((bool)item["status"]),
				Value = item["value"],
			});
		}

		var templateData = new
		{
			AppDomain = Environment.GetEnvironmentVariable("APP_DOMAIN"),
			DateIssued = GeneralHelper.GetDateInTimeZone(permit.CreatedWhen).ToString("dd/MM/yyyy @ HH:mm"),
			PermitNo = string.Format("PTW{0:000000}", permit.RunningNumber),
			General = new
			{
				Location = TemplateConvertString(jsonObject["general"]["location"]["name"]),
				Description = TemplateConvertString(jsonObject["general"]["description"]),
				StartDate = TemplateConvertString(jsonObject["general"]["startDateTime"]),
				EndDate = TemplateConvertString(jsonObject["general"]["endDateTime"]),
			},
			PermitHolder = new
			{
				Name = TemplateConvertString(jsonObject["permitHolder"]["name"]),
				Company = TemplateConvertString(jsonObject["permitHolder"]["company"]),
				NumOfStaff = TemplateConvertString(jsonObject["permitHolder"]["numOfStaff"]),
				AdditionalStaff = TemplateConvertString(jsonObject["permitHolder"]["additionalStaff"]),
			},
			MainFormCertificates = mainFormCerts,
			AttachedCertificates = new
			{
				FormA = formA,
				FormB = formB,
				FormC = formC,
				FormD = formD,
				FormE = formE,
				FormF = formF,
				FormG = formG,
				FormH = formH,
			},
			OperationInfo = new
			{
				Activities = oiActivities,
				Locations = oiLocations,
				Tools = oiTools,
				Products = oiProducts,
			},
			OperationMeasures = new
			{
				Mechanical = new
				{
					EquipmentName = TemplateConvertString(jsonObject["operationMeasures"]["mechanical"]["equipmentName"]),
					TagNumber = TemplateConvertString(jsonObject["operationMeasures"]["mechanical"]["tagNumber"]),
					PressureFree = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["mechanical"]["pressureFree"]["status"]),
					Empty = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["mechanical"]["empty"]["status"]),
					Disconnected = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["mechanical"]["disconnected"]["status"]),
					BlankOff = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["mechanical"]["blankOff"]["status"]),
					LockedOff = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["mechanical"]["lockedOff"]["status"]),
					Flushed = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["mechanical"]["flushed"]["status"]),
					Ventilated = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["mechanical"]["ventilated"]["status"]),
					Other = new
					{
						Checkbox = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["mechanical"]["other"]["status"]),
						Value = TemplateConvertString(jsonObject["operationMeasures"]["mechanical"]["other"]["value"], false, true),
					},
				},
				Electrical = new
				{
					EquipmentName = TemplateConvertString(jsonObject["operationMeasures"]["electrical"]["equipmentName"]),
					TagNumber = TemplateConvertString(jsonObject["operationMeasures"]["electrical"]["tagNumber"]),
					LockedOff = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["electrical"]["lockedOff"]["status"]),
					Isolated = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["electrical"]["isolated"]["status"]),
					IsolationCertificate = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["electrical"]["isolationCertificate"]["status"]),
					NumOfIsolationCertificate = TemplateConvertString(jsonObject["operationMeasures"]["electrical"]["numOfIsolationCertificate"]),
				},
				Instrument = new
				{
					EquipmentName = TemplateConvertString(jsonObject["operationMeasures"]["instrument"]["equipmentName"]),
					TagNumber = TemplateConvertString(jsonObject["operationMeasures"]["instrument"]["tagNumber"]),
					LockedOff = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["instrument"]["lockedOff"]["status"]),
					Isolated = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["instrument"]["isolated"]["status"]),
					Disconnected = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["instrument"]["disconnected"]["status"]),
					Bypass = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["instrument"]["bypass"]["status"]),
					ShutdownSystemOperational = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["instrument"]["shutdownSystemOperational"]["status"]),
					FireProtectionSystemOperational = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["instrument"]["fireProtectionSysOperational"]["status"]),
					AutoFireProtectionSystemOperational = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["instrument"]["autoFireProtectionSysOperational"]["status"]),
					NumOfIsolationCertificate = TemplateConvertString(jsonObject["operationMeasures"]["instrument"]["numOfIsolationCertificate"]),
					OtherMeasures = TemplateConvertString(jsonObject["operationMeasures"]["instrument"]["otherMeasures"]),
				},
				PtwHandOver = new
				{
					TerminalSafetyAwareness = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["pwtHandOver"]["terminalSafetyAwareness"]),
					PtwAwareness = GetCheckBoxImage((bool)jsonObject["operationMeasures"]["pwtHandOver"]["ptwAwareness"]),
				}
			},
			Ppe = new
			{
				SafetyGlass = GetCheckBoxImage((bool)jsonObject["ppe"]["safetyGlass"]["status"]),
				SafetyHelmet = GetCheckBoxImage((bool)jsonObject["ppe"]["safetyHelmet"]["status"]),
				FullFaceMask = GetCheckBoxImage((bool)jsonObject["ppe"]["fullFaceMask"]["status"]),
				LeatherGloves = GetCheckBoxImage((bool)jsonObject["ppe"]["leatherGloves"]["status"]),
				NeopreneGloves = GetCheckBoxImage((bool)jsonObject["ppe"]["neopreneGloves"]["status"]),
				SafetyHarness = GetCheckBoxImage((bool)jsonObject["ppe"]["safetyHarness"]["status"]),
				FallProtection = GetCheckBoxImage((bool)jsonObject["ppe"]["fallProtection"]["status"]),
				SafetyShoes = GetCheckBoxImage((bool)jsonObject["ppe"]["safetyShoes"]["status"]),
				EarProtection = GetCheckBoxImage((bool)jsonObject["ppe"]["earProtection"]["status"]),
				BreathingProtection = GetCheckBoxImage((bool)jsonObject["ppe"]["breathingProtection"]["status"]),
				OtherPpe = new
				{
					Checkbox = GetCheckBoxImage((bool)jsonObject["ppe"]["otherPpe"]["status"]),
					Value = TemplateConvertString(jsonObject["ppe"]["otherPpe"]["value"], false, true),
				},
				OtherRequiredInJHA = TemplateConvertString(jsonObject["ppe"]["otherPpeRequiredInJHA"]),
			},
			WorkArea = new
			{
				Demarcation = GetCheckBoxImage((bool)jsonObject["workArea"]["demarcation"]["status"]),
				FireExtinguisher = GetCheckBoxImage((bool)jsonObject["workArea"]["fireExtinguisher"]["status"]),
				WarningSigns = GetCheckBoxImage((bool)jsonObject["workArea"]["warningSigns"]["status"]),
				OtherPpeWorkArea = new
				{
					Checkbox = GetCheckBoxImage((bool)jsonObject["workArea"]["otherPpeWorkArea"]["status"]),
					Value = TemplateConvertString(jsonObject["workArea"]["otherPpeWorkArea"]["value"], false, true),
				},
			},
			Precautions = new
			{
				PrecautionsForWork = TemplateConvertString(jsonObject["precautions"]["precautionsForWork"]),
				AdditionalPrecautions = TemplateConvertString(jsonObject["precautions"]["additionalPrecautions"]),
				MethodOfStatementAttached = GetCheckBoxImage((bool)jsonObject["precautions"]["methodOfStatementAttached"]),
				Declaration = GetCheckBoxImage((bool)jsonObject["precautions"]["declaration"]),
				EngineeringAdviceRequired = GetCheckBoxImage((bool)jsonObject["precautions"]["engineeringAdviceRequired"]),
				PermitIssuerDeclaration = new
				{
					AuthorizedForExecution = GetCheckBoxImage((bool)jsonObject["precautions"]["permitIssuerDeclaration"]["authorizedForExecution"]),
				},
				LeadPermitIssuerDeclaration = new
				{
					AuthorizedForExecution = GetCheckBoxImage((bool)jsonObject["precautions"]["leadPermitIssuerDeclaration"]["authorizedForExecution"]),
				},
				PermitValidity = GetCheckBoxImage((bool)jsonObject["precautions"]["permitValidity"]),
			},
		};

		var template = Template.Parse(html);
		var renderedHtml = template.Render(templateData);

		//byte[] htmlBytes = System.IO.File.ReadAllBytes(renderedHtml);
		byte[] htmlBytes = Encoding.UTF8.GetBytes(renderedHtml);
		string base64 = Convert.ToBase64String(htmlBytes);

		var browserFetcher = new BrowserFetcher();
		await browserFetcher.DownloadAsync();

		await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
		{
			Headless = true,
		});

		await using var page = await browser.NewPageAsync();
		await page.GoToAsync($"data:text/html;base64,{base64}");

		await page.EmulateMediaTypeAsync(MediaType.Print);
		//await page.SetContentAsync(html);

		await page.AddStyleTagAsync(new AddTagOptions
		{
			Url = $"https://{Environment.GetEnvironmentVariable("APP_DOMAIN")}/lib/bootstrap/css/bootstrap.min.css"
		});

		await page.AddStyleTagAsync(new AddTagOptions
		{
			Url = $"https://{Environment.GetEnvironmentVariable("APP_DOMAIN")}/lib/fontawesome/css/all.min.css"
		});

		await page.AddStyleTagAsync(new AddTagOptions
		{
			Url = $"https://{Environment.GetEnvironmentVariable("APP_DOMAIN")}/fonts/roboto.css"
		});

		//var pdfContent = await page.PdfStreamAsync(new PdfOptions
		//{
		//	Format = PaperFormat.A4,
		//	PrintBackground = true,
		//	MarginOptions = new MarginOptions
		//	{
		//		Bottom = "1cm",
		//		Left = "1.5cm",
		//		Right = "1.5cm",
		//		Top = "2cm"
		//	},
		//	DisplayHeaderFooter = true,
		//	HeaderTemplate = PdfHeaderTemplate(_webHostEnvironment.WebRootPath),
		//	FooterTemplate = PdfFooterTemplate(),
		//});

		pdfResult = await page.PdfDataAsync(new PdfOptions
		{
			Format = PaperFormat.A4,
			PrintBackground = true,
			MarginOptions = new MarginOptions
			{
				Bottom = "1cm",
				Left = "1.5cm",
				Right = "1.5cm",
				Top = "2.5cm"
			},
			DisplayHeaderFooter = true,
			HeaderTemplate = PdfHeaderTemplate(_webHostEnvironment.WebRootPath, permit.Company),
			FooterTemplate = PdfFooterTemplate(),
		});

		var fileName = $"{Guid.NewGuid()}.pdf";
		var outputFullPath = Path.Combine(_webHostEnvironment.WebRootPath, "permits", fileName);

		// Save file to server
		//await System.IO.File.WriteAllBytesAsync(outputFullPath, pdfResult);

		return pdfResult;
	}


	#region "Private static functions"

	private static string PdfHeaderTemplate(string wwwroot, Company companyInfo)
	{
		var template = string.Empty;

		var logoPath = Path.Combine(wwwroot, "img", "app-logo.jpg");
		byte[] imageBytes = File.ReadAllBytes(logoPath);
		string base64 = Convert.ToBase64String(imageBytes);

		template += "<div style=\"width:750px;\">\n";
		template += "  <div style=\"display:flex;flex-direction:row;margin:0 auto;width:340px;\">\n";
		template += $"    <div style=\"margin-right:10px;margin-top:0px;\"><img style=\"height:35px;width:auto;\" src=\"data:image/jpeg;base64,{base64}\"/></div>\n";
		template += "    <div>\n";
		template += $"      <div style=\"font-family:Arial;font-size:13pt;font-weight:bold;\">{companyInfo.Description.ToUpper()}</div>\n";
		template += "      <div style=\"font-family:Arial;font-size:10pt;\">PERMIT TO WORK FORM</div>\n";
		template += "    </div>\n";
		template += "  </div>\n";
		template += "</div>\n";

		return template;
	}


	private static string PdfFooterTemplate()
	{
		var template = string.Empty;

		template += "<div style=\"text-align:right;width:800px;padding-right:30px;font-family:Arial;font-size:8pt;\">\n";
		template += "<span class=\"pageNumber\"></span>/<span class=\"totalPages\"></span>";
		template += "</div>\n";

		return template;
	}


	private static string GetCheckBoxImage(bool isChecked)
	{
		var checkBox = "empty";

		if (isChecked)
		{
			checkBox = "mark";
		}

		var imgUrl = $"{Environment.GetEnvironmentVariable("APP_DOMAIN")}/img/cb-{checkBox}.png";

		return $"<img src=\"https://{imgUrl}\" style=\"width:20px;\" />";
	}


	private static string TemplateConvertString(object data, bool useYesNo = false, bool isOtherValue = false)
	{
		var stringData = "&mdash;";

		try
		{
			var value = (JValue)data;

			if (value.Type != JTokenType.Null)
			{
				if (value.Type == JTokenType.Date)
				{
					stringData = Convert.ToDateTime(value).ToLocalTime().ToString("dd/MM/yyyy");
				}

				if (value.Type == JTokenType.String)
				{
					string tmp = value.ToString().Trim();
					stringData = (tmp == string.Empty) ? "&mdash;" : tmp;

					if (isOtherValue)
					{
						stringData = (tmp == string.Empty) ? "&nbsp;" : $"({tmp})";
					}

					stringData = stringData.Replace("\n", "<br>");
				}

				if (value.Type == JTokenType.Integer)
				{
					stringData = value.ToString();
				}

				if (value.Type == JTokenType.Boolean && useYesNo)
				{
					bool yesNo = Convert.ToBoolean(value);
					stringData = yesNo ? "YES" : "NO";
				}
			}

			return stringData;
		}
		catch (Exception ex)
		{
			var errorMessage = ex.Message;
			return stringData;
		}
	}


	private static string GetCertificateHtmlTemplate(string wwwroot, string formName)
	{
		var template = string.Empty;
		var htmlFile = Path.Combine(wwwroot, "templates", "html", "certificates", $"{formName}.html");

		template = System.IO.File.ReadAllText(htmlFile, Encoding.UTF8);

		return template;
	}

	#endregion

}
