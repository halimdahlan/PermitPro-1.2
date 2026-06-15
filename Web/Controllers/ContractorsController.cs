#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PermitPro.App.Controllers.Base;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Enums;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;

using System.Text;
using System.Text.Json;

namespace PermitPro.App.Controllers;

public class ContractorsController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;

	public ContractorsController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ISystemConfigurationService systemConfigurationService
	) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_dbContext = dbContext;
	}

	public IActionResult Index(Guid company)
	{
		var sites = _dbContext.Sites
			.AsNoTracking()
			.Include(x => x.SiteCompany)
			.Where(x => x.IsActive == true && x.SiteType == SiteTypeEnum.Site && x.SiteCompany.Id == company);

		var locations = sites
			.Select(e => new
			{
				Id = e.Id.ToString(),
				e.Name,
			})
			.ToList();

		locations.Insert(0, new { Id = string.Empty, Name = "All" });

		ViewBag.Locations = locations;

		var roleContractor = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "CONTRACTOR");

		if (roleContractor != null)
		{
			var model = new ContractorsViewModel
			{
				CompanyId = CompanyID,
				Role = roleContractor
			};

			return View(model);
		}

		return View();
	}


	public IActionResult New()
	{
		return View();
	}


	public IActionResult Edit()
	{
		return View();
	}


	[HttpGet("{company}/contractors/grid")]
	public JsonResult ContractorsGridView(Guid company)
	{
		var contractors = _dbContext.Users
			.Include(e => e.UserCompany)
			.Include(e => e.UserRoles)
			.ThenInclude(e => e.Role)
			.Include(e => e.Sites)
			.Where(e => e.UserCompany.Id == company && e.UserRoles.Any(ur => ur.Role.NormalizedName == "CONTRACTOR"))
			.OrderByDescending(e => e.CreatedWhen)
			.Select(e => new ContractorListViewModel
			{
				Id = e.Id,
				Name = string.Format("{0} {1}", e.FirstName.Trim(), e.LastName.Trim()),
				Email = e.Email,
				Location = string.Join(", ", e.Sites.Select(s => s.Name).ToList()),
				IsSecured = e.PasswordHash != null ? "<i class=\"fa-regular fa-user-lock fa-lg\"></i>" : "<i class=\"fa-regular fa-user-unlock fa-lg\"></i>",
				IsActiveIcons = e.IsActive ? "<i class=\"fa-sharp fa-solid fa-circle-check fa-lg text-success\"></i>" : "<i class=\"fa-sharp fa-solid fa-circle-check fa-lg text-warning\"></i>",
				IsActive = e.IsActive,
				CreatedWhen = GeneralHelper.GetDateInTimeZone(e.CreatedWhen),
				ActionIcons = ContractorsGridActions(company, e.Id, e.PasswordHash != null),
			});

		return new JsonResult(contractors, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}


	[HttpGet("{company}/contractors/sites/hierarchical")]
	public JsonResult GetLocationsHierarchical(Guid company, Guid? id)
	{
		Guid? parentId = Guid.Empty;
		if (id != null) parentId = id;

		var data = _dbContext.Sites
			.Include(e => e.SiteCompany)
			.Where(e => e.SiteType == SiteTypeEnum.Site && e.SiteCompany.Id == company && e.ParentId == parentId)
			.OrderBy(e => e.ParentId)
			.OrderBy(e => e.Name)
			.Select(e => new
			{
				id = e.Id,
				name = e.Name,
				hasChildren = _dbContext.Sites.Any(f => f.SiteType == SiteTypeEnum.Site && f.SiteCompany.Id == company && f.ParentId == e.Id)
			});

		return new JsonResult(data, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}


	#region "Private static functions/methods"

	private static string ContractorsGridActions(Guid company, string id, bool isSecured)
	{
		var icons = string.Empty;
		var disabled = isSecured ? " disabled" : "";

		StringBuilder sb = new();

		sb.AppendLine("<div class=\"d-flex flex-row action-icons\">");
		sb.AppendLine($"<a href=\"/{company}/users/edit/{id}?org=c\" class=\"text-secondary\"><i class=\"fa-solid fa-money-check-pen fa-lg\"></i></a>");
		sb.AppendLine($"<a href=\"javascript:;\" class=\"no-loading text-danger\" onclick=\"deleteUser('{id}')\"><i class=\"fa-solid fa-trash-xmark fa-lg\"></i></a>");
		sb.AppendLine("</div>");


		icons = sb.ToString();

		return icons;
	}

	#endregion

}
