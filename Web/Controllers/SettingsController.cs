using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using PermitPro.App.Controllers.Base;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

namespace PermitPro.App.Controllers;

public class SettingsController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;

	public SettingsController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ISystemConfigurationService systemConfigurationService
	) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_dbContext = dbContext;
	}


	public IActionResult Index()
	{
		return View();
	}


	public JsonResult GetLocations(Guid? id)
	{
		var locations = _dbContext.Sites
			.Where(e => id.HasValue ? e.ParentId == id : e.ParentId == Guid.Empty)
			.OrderBy(e => e.Name)
			.Select(e => new
			{
				id = e.Id,
				name = e.Name,
				image = $"/img/site-type/16x16/{e.SiteType.ToString().ToLower()}.png",
				siteType = e.SiteType,
				parentId = e.ParentId,
				expanded = e.Name.ToLower() == "iot",
				hasChildren = _dbContext.Sites.Where(f => f.ParentId == e.Id).Count() > 0
			})
			.ToList();

		return Json(locations.ToList());
	}
}
