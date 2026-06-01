#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

namespace PermitPro.App.Controllers.Base
{
	public class AppControllerBase : Controller
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly SignInManager<UserInfo> _signInManager;
		private readonly ISystemConfigurationService _systemConfigService;
		private Guid _companyId;

		public Guid CompanyID
		{
			get { return _companyId; }
		}


		public AppControllerBase(
			ApplicationDbContext dbContext
			, IHttpContextAccessor httpContextAccessor
			, SignInManager<UserInfo> signInManager
			, ISystemConfigurationService systemConfigurationService)
		{
			_dbContext = dbContext;
			_httpContextAccessor = httpContextAccessor;
			_signInManager = signInManager;
			_systemConfigService = systemConfigurationService;

			//Guid company = Guid.Empty;
			_companyId = Guid.Empty;

			var controllerName = (string)_httpContextAccessor.HttpContext.GetRouteValue("controller");
			var routeValue = _httpContextAccessor!.HttpContext!.GetRouteValue("company");

			if (routeValue != null)
			{
				//company = Guid.Parse(routeValue.ToString());
				_ = Guid.TryParse(routeValue.ToString(), out _companyId);

				if (_companyId == Guid.Empty)
				{
					_signInManager.SignOutAsync().Wait();
					_httpContextAccessor!.HttpContext!.Response.Redirect("/account/login");
				}
				else
				{
					if (controllerName.ToLower() != "landing" && !_systemConfigService.HasAccess(controllerName))
					{
						_httpContextAccessor!.HttpContext!.Response.Redirect($"/{_companyId}/restricted/accessdenied");
					}
				}
			}
		}
	}
}
