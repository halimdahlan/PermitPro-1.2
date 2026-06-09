using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PermitPro.App.Controllers.Base;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

using System.Security.Claims;
using System.Text.Json;

namespace PermitPro.App.Controllers;

public class CompaniesController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly ILogService _logService;

	public CompaniesController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ISystemConfigurationService systemConfigurationService
		, ILogService logService) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_dbContext = dbContext;
		_httpContextAccessor = httpContextAccessor;
		_logService = logService;
	}

	public IActionResult Index()
	{
		return View();
	}

	[HttpGet("companies/grid")]
	public JsonResult GetCompaniesGrid()
	{
		var companies = _dbContext.Companies
			.AsNoTracking()
			.OrderByDescending(e => e.CreatedWhen)
			.Select(e => new CompanyGridViewModel
			{
				Id = e.Id,
				Name = e.Name,
				Description = e.Description ?? "—",
				IsActive = e.IsActive,
				CreatedWhen = e.CreatedWhen,
				ActionIcons = CompanyGridActionIcons(e.Id, e.IsActive),
			})
			.ToList();

		return new JsonResult(companies, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}

	[HttpGet("companies/{id}/info")]
	public async Task<IActionResult> GetCompanyInfo(Guid id)
	{
		var company = await _dbContext.Companies
			.AsNoTracking()
			.FirstOrDefaultAsync(e => e.Id == id);

		if (company == null)
		{
			return NotFound();
		}

		return Ok(new
		{
			Id = company.Id,
			Name = company.Name,
			Description = company.Description ?? string.Empty,
			IsActive = company.IsActive,
		});
	}

	[HttpPost("companies/{mode}")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ManageCompany(string mode = "new")
	{
		try
		{
			var req = Request.Form;

			if (mode == "new")
			{
				var company = new Company
				{
					Id = Guid.NewGuid(),
					Name = req["Name"].ToString(),
					Description = req["Description"].ToString(),
					IsActive = Convert.ToBoolean(req["IsActive"]),
					CreatedBy = Guid.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)),
					CreatedWhen = DateTime.UtcNow,
				};

				_dbContext.Companies.Add(company);
				await _dbContext.SaveChangesAsync();
			}
			else if (mode == "edit")
			{
				var company = await _dbContext.Companies.FirstOrDefaultAsync(e => e.Id == Guid.Parse(req["Id"].ToString()));

				if (company == null)
				{
					return NotFound("Unable to find company");
				}

				company.Name = req["Name"].ToString();
				company.Description = req["Description"].ToString();
				company.IsActive = Convert.ToBoolean(req["IsActive"]);
				company.UpdatedBy = Guid.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier));
				company.UpdatedWhen = DateTime.UtcNow;

				_dbContext.Companies.Update(company);
				await _dbContext.SaveChangesAsync();
			}

			return Ok(new { Data = "OK" });
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}

	[HttpDelete("companies/{id}")]
	public async Task<IActionResult> DeleteCompany(Guid id)
	{
		try
		{
			var company = await _dbContext.Companies.FirstOrDefaultAsync(e => e.Id == id);

			if (company == null)
			{
				return NotFound();
			}

			company.IsDeleted = true;
			company.DeletedBy = Guid.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier));
			company.DeletedWhen = DateTime.UtcNow;

			_dbContext.Companies.Update(company);
			await _dbContext.SaveChangesAsync();

			return Ok(new { Data = "OK" });
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}

	#region "Private"

	private static string CompanyGridActionIcons(Guid id, bool isActive)
	{
		var icons = string.Empty;
		icons += "<div class=\"d-flex flex-row action-icons\">";
		icons += $"<a href=\"javascript:;\" onclick=\"editCompany('{id}')\" class=\"no-loading text-secondary\"><i class=\"fa-solid fa-money-check-pen fa-lg\"></i></a>";
		icons += $"<a href=\"javascript:;\" class=\"no-loading text-danger\" onclick=\"deleteCompany('{id}')\"><i class=\"fa-solid fa-trash-xmark fa-lg\"></i></a>";
		icons += "</div>";
		return icons;
	}

	#endregion
}
