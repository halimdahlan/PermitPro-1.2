using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using PermitPro.App.Controllers.Base;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

namespace PermitPro.App.Controllers;

public class LandingController : AppControllerBase
{
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly UserManager<UserInfo> _userManager;
	private readonly ISystemConfigurationService _systemConfigurationService;

	public LandingController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, UserManager<UserInfo> userManager
		, ISystemConfigurationService systemConfigurationService) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_httpContextAccessor = httpContextAccessor;
		_systemConfigurationService = systemConfigurationService;
		_userManager = userManager;
	}

	public async Task<IActionResult> Index(Guid company, string entity, string entityId)
	{
		if (company != Guid.Empty)
		{
			_systemConfigurationService.Init();

			var isContractorOrAGT = User.IsInRole("Permit Holder / Contractor") || User.IsInRole("Authorized Gas Tester");
			var controller = "dashboard";

			if (isContractorOrAGT)
			{
				controller = "permits";
			}

			if (!string.IsNullOrEmpty(entityId) && !string.IsNullOrEmpty(entity))
			{
				return LocalRedirect($"/{company}/{entity}/edit/{entityId}");
			}

			return LocalRedirect($"/{company}/{controller}");
		}

		return View();
	}
}
