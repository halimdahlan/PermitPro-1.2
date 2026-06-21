#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

using PermitPro.App.Controllers.Base;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Enums;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;

using System.Collections;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PermitPro.App.Controllers;

[Authorize]
public class PermitsController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly UserManager<UserInfo> _userManager;
	private readonly ICurrentUserService _currentUserService;
	private readonly IWebHostEnvironment _webHostEnvironment;
	private readonly IPermitService _permitService;
	private readonly ILogService _logService;

	public PermitsController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ISystemConfigurationService systemConfigurationService
		, UserManager<UserInfo> userManager
		, ICurrentUserService currentUserService
		, IWebHostEnvironment webHostEnvironment
		, ILogService logService
		, IPermitService permitService) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_userManager = userManager;
		_dbContext = dbContext;
		_currentUserService = currentUserService;
		_webHostEnvironment = webHostEnvironment;
		_permitService = permitService;
		_logService = logService;
	}

	[Route("{company}/permits/removefile")]
	public ActionResult RemoveFile()
	{
		return Content("");
	}


	public async Task<IActionResult> Index(Guid company, string filter, string field)
	{
		var model = new PermitViewModel
		{
			CompanyId = company,
			UserRoles = await _currentUserService.GetCurrentUserRoles(),
			GridFilter = filter,
			GridFilterField = field,
		};

		return View(model);
	}


	[Route("{company}/permits/edit/{id}")]
	public async Task<IActionResult> Edit(Guid company, Guid id, string origin)
	{
		if (!string.IsNullOrEmpty(origin) && origin == "email")
		{
			var returnUrl = $"/{company}/permits/edit/{id}";
			return LocalRedirectPermanent($"/account/login?url={returnUrl}");
		}

		var permit = _dbContext.Permits
			.Include(e => e.Site)
			.Include(e => e.WorkflowHistories)
			.Include(e => e.PermitWorkflowStep)
			.ThenInclude(e => e.Approvers)
			.Where(e => e.Id == id)
			.FirstOrDefault();

		var currentUser = _currentUserService.GetCurrentUser();
		var currentUserIsAdmin = await _userManager.IsInRoleAsync(currentUser, "SuperUser") || await _userManager.IsInRoleAsync(currentUser, "Administrator");

		if (permit != null)
		{
			var workflowHistories = permit.WorkflowHistories
				.OrderByDescending(e => e.CreatedWhen)
				.Select(e => new WorkflowHistoryViewModel
				{
					UserDisplayName = _dbContext.Users
						.Where(u => u.Id.ToLower() == e.CreatedBy.ToString().ToLower())
						.Select(u => new
						{
							FullName = $"{u.FirstName.Trim()} {u.LastName.Trim()}"
						})
						.Single()
						.FullName,
					DateCreated = $"{GeneralHelper.GetDateInTimeZone(e.CreatedWhen):dd MMM, yyyy} at {GeneralHelper.GetDateInTimeZone(e.CreatedWhen):hh:mm tt}",
					Status = e.Status.ToString(),
					Comments = e.Comments,
					StatusBgColor = GeneralHelper.GetAlertBgColor(e.Status),
				})
				.ToList();

			var pattern = @"'";
			var toReplace = @"\'";
			var regExOptions = RegexOptions.Multiline;
			var regEx = new Regex(pattern, regExOptions);

			var permitUpdatedByInfo = "N/A";
			var permitCreatedByInfo = "N/A";

			if (permit.WorkflowHistories.Count > 0)
			{
				var updatedBy = permit.WorkflowHistories
					.OrderByDescending(e => e.CreatedWhen)
					.Select(e => new
					{
						_dbContext.Users
							.Where(u => u.Id.ToLower() == e.CreatedBy.ToString().ToLower())
							.Select(u => new
							{
								FullName = $"{u.FirstName.Trim()} {u.LastName.Trim()}"
							})
							.Single()
							.FullName,
						UpdatedWhen = e.CreatedWhen,
					})
					.FirstOrDefault();

				permitUpdatedByInfo = $"{updatedBy.FullName} on {GeneralHelper.GetDateInTimeZone(updatedBy.UpdatedWhen):dd MMM, yyyy} @ {GeneralHelper.GetDateInTimeZone(updatedBy.UpdatedWhen):hh:mm tt}";
			}

			var createdBy = _dbContext.Users
				.Where(user => user.Id == permit.CreatedBy.ToString())
				.Select(e => new
				{
					UserFullName = $"{e.FirstName} {e.LastName}",
				})
				.FirstOrDefault();

			permitCreatedByInfo = $"{createdBy.UserFullName} on {GeneralHelper.GetDateInTimeZone(permit.CreatedWhen):dd MMM, yyyy} @ {GeneralHelper.GetDateInTimeZone(permit.CreatedWhen):hh:mm tt}";


			var model = new EditPermitViewModel
			{
				PermitNo = $"PTW{permit.RunningNumber:000000}",
				PermitId = permit.Id,
				PermitJson = regEx.Replace(permit.PermitForm, toReplace),
				PermitStatus = permit.PermitStatus,
				PermitStatusDisplay = GeneralHelper.GetPermitStatusDisplay(permit.PermitStatus, permit.PermitWorkflowStep.Name),
				WorkflowStepName = permit.PermitWorkflowStep != null ? permit.PermitWorkflowStep.Name : null,
				AlertBgColor = GeneralHelper.GetAlertBgColor(permit.PermitStatus),
				CompanyId = company,
				UserRoles = _currentUserService.GetCurrentUserRoles().Result,
				WorkflowHistories = permit.WorkflowHistories
					.OrderByDescending(e => e.CreatedWhen)
					.Select(e => new WorkflowHistoryViewModel
					{
						UserDisplayName = _dbContext.Users
							.Where(u => u.Id.ToLower() == e.CreatedBy.ToString().ToLower())
							.Select(u => new
							{
								FullName = $"{u.FirstName.Trim()} {u.LastName.Trim()}"
							})
							.Single()
							.FullName,
						DateCreated = $"{GeneralHelper.GetDateInTimeZone(e.CreatedWhen):dd MMM, yyyy} at {GeneralHelper.GetDateInTimeZone(e.CreatedWhen):hh:mm tt}",
						Status = e.Status.ToString(),
						Comments = e.Comments,
						StatusBgColor = GeneralHelper.GetAlertBgColor(e.Status),
					})
					.ToList(),
				Location = permit.Site,
				SuspendAutoResume = permit.AutoResumeSuspended,
				SuspendDate = permit.SuspendedDateTime,
				PermitUpdatedByInfo = permitUpdatedByInfo,
				PermitCreatedByInfo = permitCreatedByInfo,
			};

			if (permit.PermitWorkflowStep != null)
			{
				model.ExecuteWorkflowAction = permit.PermitWorkflowStep.Approvers.Any(e => e.Id == currentUser.Id);
			}

			if (currentUserIsAdmin)
			{
				model.ExecuteWorkflowAction = currentUserIsAdmin;
			}

			return View(model);
		}

		return NotFound();
	}


	[Route("{company}/permits/new")]
	public IActionResult New(Guid company)
	{
		var model = new PermitViewModel
		{
			CompanyId = company,
			//UserRoles = await _currentUserService.GetCurrentUserRoles(),
			EditPermitViewModel = new EditPermitViewModel
			{
				CompanyId = company,
				PermitStatus = PermitStatusEnum.Draft,
			},
		};

		return View(model);
	}


	[Route("{company}/permits/formview")]
	public IActionResult FormView()
	{
		return View();
	}


	#region "Grid"

	public async Task<JsonResult> GetGridData(Guid company, string filter)
	{
		var isContractor = await _currentUserService.IsContractor();

		var data = _dbContext.Permits
			.AsNoTracking()
			.Include(e => e.Site)
			.Include(e => e.Attachments)
			.Include(e => e.PermitWorkflowStep)
			.ThenInclude(e => e.WorkflowStepWorkflow)
			.Where(e => e.Company.Id == company)
			.OrderByDescending(e => e.CreatedWhen)
			.Select(e => new
			{
				e.Id,
				e.PermitForm,
				e.RunningNumber,
				LocationName = e.Site.Name,
				PermitStatus = e.PermitStatus.ToString().ToUpper(),
				e.CreatedWhen,
				e.CreatedBy,
				SubmittedBy = _dbContext.Users
					.Select(p => new
					{
						UserId = p.Id,
						FullName = $"{p.FirstName} {p.LastName}"
					})
					.SingleOrDefault(u => u.UserId == e.CreatedBy.ToString().ToLower())
					.FullName ?? "Unknown",
			});

		if (isContractor)
		{
			var _currentUser = _currentUserService.GetCurrentUser();
			data = data.Where(e => e.CreatedBy.ToString().ToLower() == _currentUser.Id.ToLower());
		}

		var permits = data.ToList();


		List<PermitListViewModel> result = new();

		foreach (var permit in permits)
		{
			JObject json = JObject.Parse(permit.PermitForm);

			var loc = json["general"]["location"];
			var description = json["general"]["description"];
			var dtmStart = (JValue)json["general"]["startDateTime"];
			var dtmEnd = (JValue)json["general"]["endDateTime"];

			//var tmpCertsX = json["general"]["certificates"].Children();

			List<JToken> tmpCerts = json["general"]["certificates"].Children().ToList();
			List<dynamic> certs = new();

			foreach (var cert in tmpCerts)
			{
				CertificateIcon selectedCert = cert["name"].ToString().ToLower() switch
				{
					"hotwork" => new() { Code = "A", Description = "Hot Work" },
					"confinedspace" => new() { Code = "B", Description = "Confined Space" },
					"radiation" => new() { Code = "C", Description = "Radiation" },
					"excavation" => new() { Code = "D", Description = "Excavation" },
					"isolation" => new() { Code = "E", Description = "Isolation" },
					"methodStatement" => new() { Code = "F", Description = "Method Statement" },
					"liftinghoisting" => new() { Code = "G", Description = "Lifting & Hoisting" },
					"override" => new() { Code = "H", Description = "Override" },
					_ => new() { Code = "X", Description = cert["name"].ToString() },
				};

				//var certIcon = $"<div class=\"avatar avatar-sm\"><span class=\"avatar-title rounded-circle text-dark\" data-bs-toggle=\"tooltip\" data-bs-title=\"{selectedCert.Description}\">{selectedCert.Letter}</span></div>";
				var certIcon = $"<img title=\"({selectedCert.Code}) {selectedCert.Description}\" src=\"/img/icons/certs/{cert["name"].ToString().ToLower()}.png\" style=\"width:32px;height:32px;margin-right:3px;\" />";

				certs.Add(certIcon);
			}

			var newPermit = new PermitListViewModel
			{
				Id = permit.Id,
				PermitNumber = string.Format("PTW{0:000000}", permit.RunningNumber),
				Description = GetTruncatedWords(description.ToString(), 20), //description.ToString(),
				Location = permit.LocationName,
				StartDate = dtmStart.Value != null ? DateTime.Parse(dtmStart.ToString()).ToLocalTime() : null,
				EndDate = dtmEnd.Value != null ? DateTime.Parse(dtmEnd.ToString()).ToLocalTime() : null,
				Certificates = certs.Count > 0 ? "<div class=\"d-flex flex-row certificate-icons justify-content-center\">" + string.Join("", certs) + "</div>" : "",
				Status = permit.PermitStatus,
				StatusBadge = GeneralHelper.GetStatusBadge(permit.PermitStatus.ToUpper()),
				DateSubmitted = GeneralHelper.GetDateInTimeZone(permit.CreatedWhen),
				SubmittedBy = permit.SubmittedBy,
			};

			//if (dtmStart.Value != null) newPermit.StartDate = DateTime.Parse(dtmStart.ToString()).ToLocalTime();
			//if (dtmEnd.Value != null) newPermit.EndDate = DateTime.Parse(dtmEnd.ToString()).ToLocalTime();

			result.Add(newPermit);
		}

		return new JsonResult(result, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}


	public JsonResult GetPermitStatusAll()
	{
		List<SelectListItem> list = new()
		{
			new SelectListItem
			{
				Text = "(select)",
				Value = "",
			},
			new SelectListItem
			{
				Text = "Draft",
				Value = "DRAFT",
			},
			new SelectListItem
			{
				Text = "Pending",
				Value = "PENDING",
			},
			new SelectListItem
			{
				Text = "Approved",
				Value = "APPROVED",
			},
			new SelectListItem
			{
				Text = "Rejected",
				Value = "REJECTED",
			},
			new SelectListItem
			{
				Text = "Closed",
				Value = "CLOSED",
			},
			new SelectListItem
			{
				Text = "Suspended",
				Value = "SUSPENDED",
			},
			new SelectListItem
			{
				Text = "Overdue",
				Value = "OVERDUE",
			},
		};

		return Json(list.ToList());
	}

	#endregion


	#region "GET"

	[HttpGet("{company}/permits/permit/{id}/files")]
	public IActionResult GetUploadedFiles(Guid company, Guid id)
	{
		var files = _dbContext.Attachments
			.AsNoTracking()
			.Include(f => f.Permit)
			.Where(f => f.Permit.Id == id)
			.OrderByDescending(f => f.CreatedWhen)
			.Select(f => new
			{
				PermitId = f.Permit.Id,
				f.Id,
				f.FileName,
				FileSize = $"{(f.FileSize / 1024):n0}",
				f.ContentType,
				CreatedWhen = GeneralHelper.GetDateInTimeZone(f.CreatedWhen).ToString("dd MMM, yyyy"),
				CreatedWhenTick = GeneralHelper.FormatDateTimeTicks(GeneralHelper.GetDateInTimeZone(f.CreatedWhen)),
			})
			.ToList();

		return Ok(new
		{
			Data = files,
		});
	}

	#endregion


	#region "POST"

	[HttpPost("{company}/permits")]
	public async Task<IActionResult> CreatePermit(Guid company)
	{
		try
		{
			await _permitService.CreateAsync();

			return Ok(new
			{
				Data = "OK"
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}



	[HttpPost("{company}/permits/permit/status")]
	public async Task<IActionResult> PermitStatus()
	{
		await _permitService.SetPermitStatusAsync();

		return Ok();
	}


	[HttpPost("{company}/permits/{id}/suspend")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> SuspendPermit(Guid id)
	{
		try
		{
			var req = Request.Form;
			DateTime? suspendDate = null;
			PermitStatusEnum permitStatus = PermitStatusEnum.Pending;
			PermitStatusEnum? previousPermitStatus = null;
			bool suspendAutoResume = false;
			var logMessage = "";

			var suspendState = req["SuspendState"].ToString();

			var permit = _dbContext.Permits.FirstOrDefault(e => e.Id == id);

			if (permit == null)
			{
				await _logService.LogMessageAsync(LogTypeEnum.Error, "PERMIT_SUSPEND", "Permit not found", _currentUserService.GetCurrentUser());
				return NotFound("Permit not found");
			}

			if (suspendState == "suspend")
			{
				if (string.IsNullOrEmpty(req["SuspendDate"]))
				{
					await _logService.LogMessageAsync(LogTypeEnum.Error, "PERMIT_SUSPEND", "Suspend date is required", _currentUserService.GetCurrentUser());
					return BadRequest("Suspend date is required");
				}

				var tmpDate = JObject.Parse(req["SuspendDate"]);

				suspendDate = new DateTime(
					Convert.ToInt32(tmpDate["year"]),
					Convert.ToInt32(tmpDate["month"]),
					Convert.ToInt32(tmpDate["day"]),
					0, 0, 0);

				suspendAutoResume = Convert.ToBoolean(req["SuspendAutoResume"]);
				permitStatus = PermitStatusEnum.Suspended;
				previousPermitStatus = permit.PermitStatus;
				logMessage = $"Permit PTW{permit.RunningNumber:000000} has been suspended";
			}
			else
			{
				logMessage = $"Permit PTW{permit.RunningNumber:000000} suspension has been cancelled";
				permitStatus = permit.PreviousPermitStatus ?? PermitStatusEnum.Pending;
				previousPermitStatus = null;
			}

			permit.SuspendedDateTime = suspendDate;
			permit.PermitStatus = permitStatus;
			permit.PreviousPermitStatus = previousPermitStatus;
			permit.AutoResumeSuspended = suspendAutoResume;
			permit.UpdatedWhen = DateTime.Now.ToUniversalTime();
			permit.UpdatedBy = Guid.Parse(_currentUserService.GetCurrentUser().Id);

			_dbContext.Permits.Update(permit);
			await _dbContext.SaveChangesAsync();

			await _logService.LogMessageAsync(LogTypeEnum.Error, "PERMIT_SUSPEND", logMessage, _currentUserService.GetCurrentUser());

			return Ok(new
			{
				Status = "OK",
			});
		}
		catch (Exception ex)
		{
			await _logService.LogMessageAsync(LogTypeEnum.Error, "PERMIT_SUSPEND", ex.Message, _currentUserService.GetCurrentUser());
			return BadRequest(ex.Message);
		}

	}


	[HttpPost("{company}/permits/attachments/remove")]
	public async Task<IActionResult> RemoveAttachment()
	{
		var req = Request.Form;

		try
		{
			var attachmentId = Guid.Parse(req["FileId"]);
			var attachment = _dbContext.Attachments
				.Include(e => e.Permit)
				.FirstOrDefault(e => e.Id == attachmentId);

			if (attachment != null)
			{
				_dbContext.Attachments.Remove(attachment);
				await _dbContext.SaveChangesAsync();

				// Delete physical file from server
				var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "attachments", "permits", attachment.Permit.Id.ToString().ToLower(), attachment.FileName);

				if (System.IO.File.Exists(filePath))
				{
					System.IO.File.Delete(filePath);
				}
			}

			return Ok(new
			{
				Status = "OK",
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}

	}

	#endregion


	#region "PUT"

	[HttpPut("{company}/permits")]
	public async Task<IActionResult> UpdatePermit(Guid company)
	{
		var req = Request.Form;

		try
		{
			await _permitService.UpdateAsync();

			return Ok(new
			{
				Data = "OK"
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}

	}


	[HttpPut("{company}/permits/certs")]
	public async Task<IActionResult> UpdatePermitCertificate(Guid company)
	{
		var req = Request.Form;

		try
		{
			await _permitService.UpdateCertificateAsync();

			return Ok(new
			{
				Data = "OK"
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}

	}


	[HttpPut("{company}/permits/closed")]
	public async Task<IActionResult> MarkPermitAsClosed()
	{
		try
		{
			await _permitService.ClosePermitAsync();

			return Ok(new
			{
				Result = true,
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}

	#endregion


	#region "DELETE"

	[HttpDelete("{company}/permits/{id}")]
	public async Task<IActionResult> DeletePermit(Guid company, Guid id)
	{
		try
		{
			var permit = _dbContext.Permits.FirstOrDefault(e => e.Id == id);
			await _permitService.DeleteAsync(permit);

			return Ok(new
			{
				Data = "OK"
			});
		}
		catch (Exception ex)
		{
			return BadRequest(new
			{
				ex.Message
			});
		}
	}

	#endregion


	#region "PDF"

	[HttpGet("{company}/permits/templates/permit")]
	public IActionResult GetPermitHtmlTemplate()
	{
		var htmlTemplate = System.IO.File.ReadAllText(_webHostEnvironment.WebRootPath + "/templates/html/permit-main-div-only.html");

		return Ok(new
		{
			Data = htmlTemplate
		});
	}


	[HttpPost("{company}/permits/permit/export/pdf")]
	public async Task<IActionResult> ExportPdf(Guid company)
	{
		var pdf = await _permitService.GetPdfBytesAsync(Request.Form);
		return File(pdf, "application/pdf", $"{Guid.NewGuid()}.pdf");
	}

	#endregion


	#region "Private static functions/methods"

	private string GetTruncatedWords(string text, int wordCount)
	{
		if (string.IsNullOrWhiteSpace(text)) return text;

		// Split by whitespace and remove any empty entries
		var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

		// If the text is shorter than the limit, return it as-is
		if (words.Length <= wordCount) return text;

		// Join the first N words and append ellipsis
		return string.Join(" ", words.Take(wordCount)) + " ...";
	}


	private Hashtable GetCertificateIcons()
	{
		Hashtable ht = new();
		ht.Add("hotwork", new CertificateIcon { Code = "A", Description = "Hot Work" });
		ht.Add("confinedspace", new CertificateIcon { Code = "B", Description = "Confined Space" });
		ht.Add("radiation", new CertificateIcon { Code = "C", Description = "Radiation" });
		ht.Add("excavation", new CertificateIcon { Code = "D", Description = "Excavation" });
		ht.Add("isolation", new CertificateIcon { Code = "E", Description = "Isolation" });
		ht.Add("methodStatement", new CertificateIcon { Code = "F", Description = "Method Statement" });
		ht.Add("liftingHoisting", new CertificateIcon { Code = "G", Description = "Lifting & Hoisting" });
		ht.Add("override", new CertificateIcon { Code = "H", Description = "Override" });
		return ht;
	}


	private record CertificateIcon
	{
		public string Code { get; init; }
		public string Description { get; init; }
	}

	#endregion

}
