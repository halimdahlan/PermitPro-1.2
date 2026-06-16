#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PermitPro.App.Controllers.Base;
using PermitPro.App.Models.Ajax;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Enums;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;

using System.Text.Json;

namespace PermitPro.App.Controllers;

[Authorize]
public class WorkflowController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly UserManager<UserInfo> _userManager;
	private readonly ICurrentUserService _currentUserService;

	public WorkflowController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ISystemConfigurationService systemConfigurationService
		, UserManager<UserInfo> userManager
		, ICurrentUserService currentUserService
		) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_userManager = userManager;
		_dbContext = dbContext;
		_currentUserService = currentUserService;
	}

	#region "Views"

	public IActionResult Index()
	{
		return View();
	}


	public IActionResult New()
	{
		return View();
	}


	[Route("{company}/workflow/edit/{id}")]
	public IActionResult Edit(string id)
	{
		var workflow = _dbContext.Workflows
			.Include(e => e.WorkflowSteps)
			.Where(e => e.Id.ToString().ToLower() == id)
			.Select(e => new WorkflowEditViewModel
			{
				WorkflowId = e.Id.ToString().ToLower(),
				WorkflowName = e.Name,
				WorkflowDescription = e.Description,
				WorkflowIsActive = e.IsActive,
				WorkflowHasCertificate = e.HasCertificate,
			})
			.FirstOrDefault();

		return View(workflow);
	}


	public IActionResult StepNew()
	{
		return View();
	}


	public IActionResult StepEdit()
	{
		return View();
	}

	#endregion


	#region "API"

	#region "GET"

	[HttpGet("{company}/workflow/workflows")]
	public IActionResult GetWorkflows(Guid company)
	{
		var workflows = _dbContext.Workflows
			.Include(e => e.WorkflowCompany)
			.Where(e => e.WorkflowCompany.Id == company)
			.OrderByDescending(e => e.CreatedWhen)
			.Select(e => new
			{
				e.Id,
				WorkflowName = e.Name,
				WorkflowDesc = e.Description,
				e.IsActive,
				e.HasCertificate,
				CreatedWhen = GeneralHelper.GetDateInTimeZone(e.CreatedWhen).ToString("dd MMM, yyyy"),
				UpdatedWhen = e.UpdatedWhen.Value.ToLocalTime().ToString("dd MMM, yyyy"),
				CreatedWhenTicks = GeneralHelper.FormatDateTimeTicks(GeneralHelper.GetDateInTimeZone(e.CreatedWhen)),
				UpdatedWhenTicks = GeneralHelper.FormatDateTimeTicks(e.UpdatedWhen),
			})
			.ToList();

		return Ok(new
		{
			Data = workflows
		});
	}


	[HttpGet("{company}/workflow/workflows/{workflowId}/steps")]
	public ActionResult GetWorkflowSteps(Guid company, Guid workflowId)
	{
		var workflowSteps = _dbContext.WorkflowSteps
			.Where(e => e.WorkflowStepWorkflow.Id == workflowId)
			.OrderBy(e => e.StepOrder)
			.Select(e => new
			{
				e.Id,
				e.Name,
				e.Description,
				e.Duration,
				DurationType = e.DurationType.ToString(),
				e.StepOrder,
				e.IsFirst,
				e.IsLast,
				NumOfSteps = _dbContext.WorkflowSteps.Count(x => x.WorkflowStepWorkflow.Id == workflowId)
			})
			.ToList();

		return Ok(new
		{
			Data = workflowSteps
		});
	}


	[HttpGet("{company}/workflow/steps/{id}")]
	public async Task<ActionResult<object>> GetWorkflowStepById(Guid company, Guid id)
	{
		var workflowStep = await _dbContext.WorkflowSteps
			.Include(e => e.Approvers)
			.Select(e => new
			{
				e.Id,
				e.Name,
				e.Description,
				e.Duration,
				e.DurationType,
				e.StepOrder,
				e.IsFirst,
				e.IsLast,
				e.AllowDelete,
				e.AllowMove,
				e.CreatedBy,
				CreatedWhen = GeneralHelper.GetDateInTimeZone(e.CreatedWhen).ToString("dd/MM/yyyy"),
				e.UpdatedBy,
				UpdatedWhen = e.UpdatedWhen.Value.ToLocalTime().ToString("dd/MM/yyyy"),
				CreateWhenTicks = GeneralHelper.FormatDateTimeTicks(GeneralHelper.GetDateInTimeZone(e.CreatedWhen)),
				UpdatedWhenTicks = GeneralHelper.FormatDateTimeTicks(e.UpdatedWhen),
				Approvers = e.Approvers
					.Select(f => new
					{
						f.Id,
						f.FirstName,
						f.LastName,
						f.Email
					})
					.ToList()
			})
			.FirstOrDefaultAsync(e => e.Id == id);

		return workflowStep;
	}


	[HttpGet("{company}/workflow/grid")]
	public JsonResult GetWorkflowsGrid(Guid company)
	{
		var workflows = _dbContext.Workflows
			.Include(e => e.WorkflowCompany)
			.Where(e => e.WorkflowCompany.Id == company)
			.OrderByDescending(e => e.CreatedWhen)
			.Select(e => new WorkflowGridViewModel
			{
				Id = e.Id,
				Name = e.Name,
				Description = e.Description,
				IsActive = e.IsActive,
				HasCertificates = e.HasCertificate,
				CreatedWhen = GeneralHelper.GetDateInTimeZone(e.CreatedWhen),
				ActionIcons = WorkflowsGridActionIcons(e.Id.ToString(), company),
			})
			.ToList();

		return new JsonResult(workflows, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}

	#endregion


	#region "POST"

	[HttpPost("{company}/workflow/workflows")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CreateWorkflow(Guid company)
	{
		try
		{
			var comp = _dbContext.Companies.SingleOrDefault(e => e.Id == company);
			var req = Request.Form;

			var workflow = new Workflow
			{
				Name = req["Name"],
				Description = req["Description"],
				IsActive = Convert.ToBoolean(req["IsActive"]),
				HasCertificate = Convert.ToBoolean(req["HasCertificate"]),
				WorkflowCompany = comp,
			};

			var draftStep = new WorkflowStep
			{
				Name = "Draft",
				Description = "Draft",
				AllowDelete = false,
				AllowMove = false,
				StepOrder = 0,
				IsFirst = true,
				IsLast = false,
				WorkflowStepWorkflow = workflow,
			};

			workflow.WorkflowSteps.Add(draftStep);

			var doneStep = new WorkflowStep
			{
				Name = "Completed",
				Description = "Completed",
				AllowDelete = false,
				AllowMove = false,
				StepOrder = 100,
				IsFirst = false,
				IsLast = true,
				WorkflowStepWorkflow = workflow,
			};

			workflow.WorkflowSteps.Add(doneStep);

			_dbContext.Workflows.Add(workflow);

			await _dbContext.SaveChangesAsync();

			return Ok(new
			{
				HasError = false,
				WorkflowId = workflow.Id,
			});
		}
		catch (Exception ex)
		{
			return BadRequest(new
			{
				HasError = true,
				ErrorMessage = ex.Message
			});
		}

	}


	[HttpPost("{company}/workflow/workflows/{workflowId}/steps")]
	[ValidateAntiForgeryToken]
	public async Task<ActionResult> CreateWorkflowStep(Guid company, Guid workflowId)
	{
		try
		{
			var req = Request.Form;

			var workflow = _dbContext.Workflows.SingleOrDefault(e => e.Id == workflowId);
			var currentStep = _dbContext.WorkflowSteps
				.Where(e => e.WorkflowStepWorkflow == workflow && !e.IsLast)
				.Max(e => e.StepOrder);

			currentStep++;

			DurationTypeEnum durType = DurationTypeEnum.Minute;

			switch (Convert.ToInt16(req["DurationType"]))
			{
				case 0:
					durType = DurationTypeEnum.Minute;
					break;
				case 1:
					durType = DurationTypeEnum.Hour;
					break;
				case 2:
					durType = DurationTypeEnum.Day;
					break;
			}

			var workflowStep = new WorkflowStep
			{
				Name = req["Name"],
				Description = req["Description"],
				Duration = Convert.ToInt16(req["Duration"]),
				DurationType = durType,
				StepOrder = currentStep,
				AllowDelete = true,
				AllowMove = true,
				IsFirst = false,
				IsLast = false,
				WorkflowStepWorkflow = workflow
			};

			_dbContext.WorkflowSteps.Add(workflowStep);
			await _dbContext.SaveChangesAsync();

			return Ok(new
			{
				HasError = false
			});
		}
		catch (Exception ex)
		{
			return Ok(new
			{
				HasError = true,
				ErrorMessage = ex.Message
			});
		}

	}

	#endregion


	#region "PUT"

	[HttpPut("{company}/workflow/workflows/{id}/edit")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateWorkflow(Guid company, Guid id)
	{
		try
		{
			var req = Request.Form;
			var workflow = _dbContext.Workflows.SingleOrDefault(e => e.Id == id);

			workflow.Name = req["Name"];
			workflow.Description = req["Description"];
			workflow.IsActive = Convert.ToBoolean(req["IsActive"]);
			workflow.HasCertificate = Convert.ToBoolean(req["HasCertificate"]);

			_dbContext.Workflows.Update(workflow);
			await _dbContext.SaveChangesAsync();

			return Ok(new
			{
				HasError = false,
				WorkflowId = workflow.Id,
			});
		}
		catch (Exception ex)
		{
			return Ok(new
			{
				HasError = true,
				ErrorMessage = ex.Message
			});
		}

	}


	[HttpPut("{company}/workflow/steps/{id}/approvers")]
	[ValidateAntiForgeryToken]
	public async Task<ActionResult> UpdateWorkflowStepApprovers(Guid company, Guid id)
	{
		var req = Request.Form;
		var approvers = req["Approvers"].ToString().Split(",");

		var workflowStep = _dbContext.WorkflowSteps
			.Include(e => e.Approvers)
			.SingleOrDefault(e => e.Id == id);

		if (workflowStep != null)
		{
			workflowStep.Approvers.Clear();

			foreach (var userId in approvers)
			{
				var user = _dbContext.Users.SingleOrDefault(u => u.Id == userId);

				if (user != null)
				{
					workflowStep.Approvers.Add(user);
				}
			}

			_dbContext.WorkflowSteps.Update(workflowStep);
			await _dbContext.SaveChangesAsync();

			return Ok(new
			{
				Data = "OK"
			});
		}

		return BadRequest(new
		{
			ErrorMessage = "Workflow step not found"
		});
	}


	[HttpPut("{company}/workflow/workflows/{workflowId}/steps/{stepId}/move")]
	public async Task<ActionResult> StepMove(Guid company, AjaxStepMoveRequest request, Guid workflowId, Guid stepId)
	{
		var workflowStep = _dbContext.WorkflowSteps.SingleOrDefault(e => e.Id == stepId);

		if (workflowStep != null)
		{
			int moveTo = 0;

			if (request.MoveUp)
			{
				moveTo = request.Previous;
			}
			else
			{
				moveTo = request.Next;
			}

			var stepToMove = _dbContext.WorkflowSteps.SingleOrDefault(e => e.WorkflowStepWorkflow.Id == workflowId && e.StepOrder == moveTo);

			stepToMove.StepOrder = request.Current;
			_dbContext.Update(stepToMove);

			workflowStep.StepOrder = moveTo;
			_dbContext.Update(workflowStep);

			await _dbContext.SaveChangesAsync();

			return Ok(new
			{
				HasError = false
			});
		}

		return NotFound();
	}


	[HttpPut("{company}/workflow/workflows/steps")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> UpdateWorkflowStep(Guid company)
	{
		try
		{
			var req = Request.Form;
			var workflowStep = _dbContext.WorkflowSteps.SingleOrDefault(e => e.Id == Guid.Parse(req["Id"]));

			if (workflowStep == null)
			{
				return BadRequest("Workflow step not found");
			}

			DurationTypeEnum durType = DurationTypeEnum.Minute;

			switch (Convert.ToInt16(req["DurationType"]))
			{
				case 0:
					durType = DurationTypeEnum.Minute;
					break;
				case 1:
					durType = DurationTypeEnum.Hour;
					break;
				case 2:
					durType = DurationTypeEnum.Day;
					break;
			}

			workflowStep.Name = req["Name"];
			workflowStep.Description = req["Description"];
			workflowStep.Duration = Convert.ToInt16(req["Duration"]);
			workflowStep.DurationType = durType;

			_dbContext.WorkflowSteps.Update(workflowStep);
			await _dbContext.SaveChangesAsync();

			return Ok(new
			{
				Success = true,
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}

	#endregion


	#region "DELETE"

	[HttpDelete("{company}/workflow/workflows/{id}")]
	public async Task<ActionResult> DeleteWorkflow(Guid company, Guid id)
	{
		try
		{
			var workflow = _dbContext.Workflows.SingleOrDefault(e => e.Id == id);

			_dbContext.Workflows.Remove(workflow);
			//await _dbContext.SoftDeleteAsync(workflow, Guid.Parse(_currentUserService.GetCurrentUser().Id));

			await _dbContext.SaveChangesAsync();

			return Ok(new
			{
				HasError = false
			});
		}
		catch (Exception ex)
		{
			return Ok(new
			{
				HasError = true,
				ErrorMessage = ex.Message
			});
		}
	}


	[HttpDelete("{company}/workflow/workflows/{workflowId}/steps/{stepId}")]
	public async Task<ActionResult> DeleteWorkflowStep(Guid company, Guid workflowId, Guid stepId)
	{
		try
		{
			var workflowStep = _dbContext.WorkflowSteps.SingleOrDefault(e => e.Id == stepId);
			_dbContext.WorkflowSteps.Remove(workflowStep);

			await _dbContext.SaveChangesAsync();

			// Get remaining workflow steps to re-arrange order
			var workflowSteps = _dbContext.WorkflowSteps
				.Where(e => e.WorkflowStepWorkflow.Id == workflowId && e.StepOrder != 0 && e.StepOrder != 100)
				.Select(e => new
				{
					e.Id
				})
				.ToList();

			int counter = 1;

			foreach (var step in workflowSteps)
			{
				workflowStep = _dbContext.WorkflowSteps.SingleOrDefault(e => e.Id == step.Id);
				workflowStep.StepOrder = counter;

				_dbContext.WorkflowSteps.Update(workflowStep);

				counter++;
			}

			await _dbContext.SaveChangesAsync();

			return Ok(new
			{
				HasError = false
			});
		}
		catch (Exception ex)
		{
			return Ok(new
			{
				HasError = true,
				ErrorMessage = ex.Message
			});
		}
	}


	[HttpDelete("{company}/workflow/steps/{workflowStepId}/approvers/{userId}")]
	public async Task<ActionResult> DeleteWorkflowStepApprover(Guid company, Guid workflowStepId, string userId)
	{
		var workflowStep = _dbContext.WorkflowSteps
			.Include(e => e.Approvers)
			.SingleOrDefault(e => e.Id == workflowStepId);

		var user = await _userManager.FindByIdAsync(userId);

		workflowStep.Approvers.Remove(user);

		_dbContext.WorkflowSteps.Update(workflowStep);
		await _dbContext.SaveChangesAsync();

		return Ok();
	}

	#endregion

	#endregion


	#region "Private static functions/methods"

	private static string WorkflowsGridActionIcons(string id, Guid companyId)
	{
		var icons = string.Empty;

		icons += "<div class=\"d-flex flex-row action-icons justify-content-center\">";
		icons += $"<a href=\"/{companyId}/workflow/edit/{id}\" class=\"no-loading text-secondary\"><i class=\"fa-solid fa-money-check-pen fa-lg\"></i></a>";
		icons += $"<a href=\"javascript:;\" class=\"no-loading text-danger\" onclick=\"deleteItem('{id}')\"><i class=\"fa-solid fa-trash-xmark fa-lg\"></i></a>";
		icons += "</div>";

		return icons;
	}

	#endregion

}
