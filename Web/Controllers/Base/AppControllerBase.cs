#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

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
					//if (controllerName.ToLower() != "landing" && !_systemConfigService.HasAccess(controllerName))
					//{
					//	_httpContextAccessor!.HttpContext!.Response.Redirect($"/{_companyId}/restricted/accessdenied");
					//}
				}
			}
		}

		private static readonly TimeSpan CompanyMetaCacheDuration = TimeSpan.FromMinutes(15);

		// Makes CompanyID and company branding available in every view.
		// Called by MVC after construction, so ViewData is writable here.
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			ViewData["CompanyId"] = CompanyID;
			ViewData["CompanyIdStr"] = CompanyID.ToString();

			if (_companyId != Guid.Empty)
			{
				var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
				var cacheKey = $"company:{_companyId}:meta";

				(string Name, string LogoFileName)? meta = null;

				if (cache != null && cache.TryGetValue(cacheKey, out (string Name, string LogoFileName) cached))
				{
					meta = cached;
				}
				else
				{
					var company = _dbContext.Companies
						.Where(c => c.Id == _companyId)
						.Select(c => new { c.Name, c.LogoFileName })
						.FirstOrDefault();

					if (company != null)
					{
						meta = (company.Name, company.LogoFileName);
						cache?.Set(cacheKey, meta.Value, CompanyMetaCacheDuration);
					}
				}

				if (meta.HasValue)
				{
					ViewData["CompanyName"] = meta.Value.Name;
					ViewData["CompanyLogoFileName"] = meta.Value.LogoFileName;
				}
			}

			base.OnActionExecuting(context);
		}

		// Call this from AdminController after editing or toggling a company.
		public static void InvalidateCompanyMetaCache(IMemoryCache cache, Guid companyId)
			=> cache?.Remove($"company:{companyId}:meta");
	}
}
