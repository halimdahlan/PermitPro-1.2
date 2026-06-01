using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;
using PermitPro.App.Controllers.Base;
using PermitPro.Core.Data;

namespace PermitPro.App.Controllers;

public class LocationsController : AppControllerBase
{
	private readonly SignInManager<UserInfo> _signInManager;
	private readonly UserManager<UserInfo> _userManager;
	private readonly ILogger<LocationsController> _logger;
	private readonly IConfiguration _configuration;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly ApplicationDbContext _dbContext;

	public LocationsController(
		UserManager<UserInfo> userManager
		, SignInManager<UserInfo> signInManager
		, ILogger<LocationsController> logger
		, IConfiguration configuration
		, IHttpContextAccessor httpContextAccessor
		, IWebHostEnvironment webHostEnvironment
		, ISystemConfigurationService systemConfigurationService
		, ApplicationDbContext dbContext) : base(httpContextAccessor, signInManager, systemConfigurationService)
	{
		_userManager = userManager;
		_signInManager = signInManager;
		_logger = logger;
		_configuration = configuration;
		_httpContextAccessor = httpContextAccessor;
		_dbContext = dbContext;
	}

	public IActionResult Index()
	{
		return View();
	}
}
