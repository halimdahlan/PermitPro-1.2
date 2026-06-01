using Microsoft.AspNetCore.Mvc;

using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

namespace PermitPro.App.Controllers;

public class ToolsController : Controller
{
	private readonly ApplicationDbContext _dbContext;
	private readonly ICurrentUserService _currentUserService;

	public ToolsController(
		ApplicationDbContext dbContext
		, ICurrentUserService currentUserService)
	{
		_dbContext = dbContext;
		_currentUserService = currentUserService;
	}

	public IActionResult Index()
	{
		var currentUser = _currentUserService.GetCurrentUser();

		var rolePortalAdmin = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "PORTALADMIN");
		var roleContractor = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "CONTRACTOR");
		var rolePermitIssuer = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "PERMITISSUER");
		var roleLeadPermitIssuer = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "LEADPERMITISSUER");
		var roleAGT = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "AUTHORIZEDGASTESTER");
		var roleSuperUser = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "SUPERUSER");

		var parentId = Guid.Parse("da9bf00e-e378-45bc-89ef-c92150faeed3");

		List<SystemMenu> systemMenus = new()
		{
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Dashboard",
				Description = "Dashboard",
				ControllerName = "Dashboard",
				ActionName = "Index",
				IconName = "fa-regular fa-chart-line",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!, rolePermitIssuer!, roleLeadPermitIssuer!
				},
				MenuOrder = 1,
				CreatedBy = Guid.Parse(currentUser.Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Permits",
				Description = "Permits",
				ControllerName = "Permits",
				ActionName = "Index",
				IconName = "fa-sharp fa-regular fa-address-card",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!, roleContractor!, rolePermitIssuer!, roleLeadPermitIssuer!, roleAGT!
				},
				MenuOrder = 2,
				CreatedBy = Guid.Parse(currentUser.Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Contractors",
				Description = "Contractors",
				ControllerName = "Contractors",
				ActionName = "Index",
				IconName = "fa-regular fa-user-helmet-safety",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!, rolePermitIssuer!, roleLeadPermitIssuer!
				},
				MenuOrder = 3,
				CreatedBy = Guid.Parse(currentUser.Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Reports",
				Description = "Reports",
				ControllerName = "Reports",
				ActionName = "Index",
				IconName = "fa-sharp fa-regular fa-file-invoice",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!, rolePermitIssuer!, roleLeadPermitIssuer!
				},
				MenuOrder = 4,
				CreatedBy = Guid.Parse(currentUser.Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Notifications",
				Description = "Notifications",
				ControllerName = "Notifications",
				ActionName = "Index",
				IconName = "fa-sharp fa-solid fa-envelope-open-text",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!, roleContractor!, rolePermitIssuer!, roleLeadPermitIssuer!, roleAGT!
				},
				MenuOrder = 5,
				CreatedBy = Guid.Parse(currentUser.Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
			new SystemMenu
			{
				Id = parentId,
				Name = "Settings",
				Description = "Settings",
				ControllerName = "Settings",
				ActionName = "Index",
				IconName = "fa-regular fa-sliders",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!
				},
				MenuOrder = 6,
				CreatedBy = Guid.Parse(currentUser.Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Location",
				Description = "Location",
				ControllerName = "Sites",
				ActionName = "Index",
				IconName = "fa-sharp fa-regular fa-map-location-dot",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!
				},
				ParentId = parentId,
				MenuOrder = 7,
				CreatedBy = Guid.Parse(currentUser.Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Users",
				Description = "Users",
				ControllerName = "Users",
				ActionName = "Index",
				IconName = "fa-solid fa-user-gear",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!
				},
				ParentId = parentId,
				MenuOrder = 8,
				CreatedBy = Guid.Parse(currentUser.Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Workflow",
				Description = "Workflow",
				ControllerName = "Workflow",
				ActionName = "Index",
				IconName = "fa-regular fa-people-carry-box",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!
				},
				ParentId = parentId,
				MenuOrder = 9,
				CreatedBy = Guid.Parse(currentUser.Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
		};

		//_dbContext.SystemMenus.AddRange(systemMenus);
		//await _dbContext.SaveChangesAsync();

		return View();
	}


	public async Task<IActionResult> EditSystemMenusAsync()
	{
		var currentUser = _currentUserService.GetCurrentUser();

		var rolePortalAdmin = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "PORTALADMIN");
		var roleContractor = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "CONTRACTOR");
		var rolePermitIssuer = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "PERMITISSUER");
		var roleLeadPermitIssuer = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "LEADPERMITISSUER");
		var roleAGT = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "AUTHORIZEDGASTESTER");
		var roleSuperUser = _dbContext.Roles.FirstOrDefault(e => e.NormalizedName == "SUPERUSER");

		var dashboard = _dbContext.SystemMenus.Where(x => x.Name == "Dashboard").FirstOrDefault();

		List<SystemMenu> menus = new()
		{
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Site Map",
				Description = "Site Map",
				ParentId = dashboard!.Id,
				ControllerName = "Sitemap",
				ActionName = "Index",
				IconName = "fa-regular fa-location-pin-lock",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!, roleContractor!, rolePermitIssuer!, roleLeadPermitIssuer!, roleAGT!
				},
				MenuOrder = 11,
				CreatedBy = Guid.Parse(_currentUserService.GetCurrentUser().Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			},
			new SystemMenu
			{
				Id = Guid.NewGuid(),
				Name = "Summary",
				ParentId = dashboard!.Id,
				Description = "Summary",
				ControllerName = "Dashboard",
				ActionName = "Index",
				IconName = "fa-duotone fa-list",
				Roles = new List<Role>
				{
					rolePortalAdmin!, roleSuperUser!, roleContractor!, rolePermitIssuer!, roleLeadPermitIssuer!, roleAGT!
				},
				MenuOrder = 10,
				CreatedBy = Guid.Parse(_currentUserService.GetCurrentUser().Id),
				CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
			}
		};

		_dbContext.SystemMenus.AddRange(menus);
		await _dbContext.SaveChangesAsync();

		return RedirectToAction("EditSystemMenus");
	}
}
