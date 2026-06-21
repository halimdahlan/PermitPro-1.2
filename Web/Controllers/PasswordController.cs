using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using PermitPro.App.Controllers.Base;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

namespace PermitPro.App.Controllers;

[Authorize]
public class PasswordController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly SignInManager<UserInfo> _signInManager;
	private readonly ISystemConfigurationService _systemConfiguration;
	private readonly UserManager<UserInfo> _userManager;
	private readonly RoleManager<Role> _roleManager;
	private readonly IWebHostEnvironment _webHostEnvironment;

	//private readonly JsonSerializerOptions _jsonOptions;

	public PasswordController(
		ApplicationDbContext dbContext
		, IHttpContextAccessor httpContextAccessor
		, SignInManager<UserInfo> signInManager
		, ISystemConfigurationService systemConfigurationService
		, UserManager<UserInfo> userManager
		, RoleManager<Role> roleManager
		, IWebHostEnvironment webHostEnvironment
	) : base(dbContext, httpContextAccessor, signInManager, systemConfigurationService)
	{
		_dbContext = dbContext;
		_httpContextAccessor = httpContextAccessor;
		_signInManager = signInManager;
		_systemConfiguration = systemConfigurationService;
		_userManager = userManager;
		_roleManager = roleManager;
		_webHostEnvironment = webHostEnvironment;
	}


	[HttpGet("/{company}/password/{id}")]
	public IActionResult Index()
	{
		return View();
	}


	[HttpPost("/{company}/password/{id}")]
	public async Task<IActionResult> Index(Guid company, Guid id, ManageUserPasswordViewModel model)
	{
		var actionName = "users";

		if (!ModelState.IsValid)
			return View(model);

		var user = await _userManager.FindByIdAsync(id.ToString());

		if (user == null)
			return NotFound("User not found!");

		user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, model.ConfirmPassword!);
		await _userManager.UpdateAsync(user);

		TempData["SuccessMessage"] = "User password has been successfully updated.";

		if (model.IsContractors) actionName = "contractors";

		return Redirect($"/{company}/{actionName}");
	}

}