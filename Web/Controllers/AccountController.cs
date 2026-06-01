#nullable disable

using MailKit.Net.Smtp;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using MimeKit;

using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;
using PermitPro.App.ViewModels;

using Scriban;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace PermitPro.App.Controllers;

public class AccountController : Controller
{

	private readonly SignInManager<UserInfo> _signInManager;
	private readonly UserManager<UserInfo> _userManager;
	private readonly IUserStore<UserInfo> _userStore;
	private readonly IUserEmailStore<UserInfo> _emailStore;
	private readonly ILogger<AccountController> _logger;
	private readonly IConfiguration _configuration;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly ApplicationDbContext _context;
	private readonly ICurrentUserService _currentUserService;
	private readonly IWebHostEnvironment _webHostEnvironment;
	private readonly EmailSettings _emailSettings;
	private readonly ILogService _logService;


	public AccountController(
		UserManager<UserInfo> userManager
		, IUserStore<UserInfo> userStore
		, SignInManager<UserInfo> signInManager
		, ILogger<AccountController> logger
		, IConfiguration configuration
		, IHttpContextAccessor httpContextAccessor
		, ApplicationDbContext context
		, ICurrentUserService currentUserService
		, IWebHostEnvironment webHostEnvironment
		, ISystemConfigurationService systemConfigurationService
		, EmailSettings emailSettings
		, ILogService logService)
	{
		_userManager = userManager;
		_userStore = userStore;
		_emailStore = GetEmailStore();
		_signInManager = signInManager;
		_logger = logger;
		_configuration = configuration;
		_httpContextAccessor = httpContextAccessor;
		_context = context;
		_currentUserService = currentUserService;
		_webHostEnvironment = webHostEnvironment;
		_emailSettings = emailSettings;
		_logService = logService;

		Guid company = Guid.Empty;
		var routeValue = _httpContextAccessor!.HttpContext!.GetRouteValue("company");

		if (routeValue != null)
		{
			Guid.TryParse(routeValue.ToString(), out company);

			if (company == Guid.Empty)
			{
				_signInManager.SignOutAsync();
				_httpContextAccessor!.HttpContext!.Response.Redirect("/account/login");
			}
		}
	}

	#region "GET"

	[HttpGet("account/login")]
	public IActionResult Login()
	{
		var companies = _context.Companies
			.Where(x => x.IsActive == true)
			.Select(x => new
			{
				x.Id,
				x.Name,
			})
			.ToList();

		ViewBag.CompanyList = companies;

		var company = _httpContextAccessor.HttpContext.Request.Query.FirstOrDefault(f => f.Key.ToLower() == "company");
		var entity = _httpContextAccessor.HttpContext.Request.Query.FirstOrDefault(f => f.Key.ToLower() == "entity");
		var entityId = _httpContextAccessor.HttpContext.Request.Query.FirstOrDefault(f => f.Key.ToLower() == "id");
		var origin = _httpContextAccessor.HttpContext.Request.Query.FirstOrDefault(f => f.Key.ToLower() == "origin");

		var model = new SignInViewModel
		{
			Email = "",
			Password = "",
			CompanyId = company.Value,
			Entity = entity.Value,
			EntityId = entityId.Value,
			Origin = origin.Value,
		};

		return View(model);
	}


	[HttpGet("account/logout")]
	public async Task<IActionResult> Logout()
	{
		await _signInManager.SignOutAsync();
		return LocalRedirect("/account/login");
	}


	[HttpGet("account/forgot")]
	public IActionResult ForgotPassword()
	{
		return View(new ForgotPasswordViewModel { Email = string.Empty, });
	}


	[HttpGet("account/reset-password")]
	public IActionResult ResetPassword(string id, string token)
	{
		var model = new ResetPasswordViewModel
		{
			Password = string.Empty,
			ConfirmPassword = string.Empty,
			Id = id,
			ResetToken = token
		};

		return View(model);
	}


	[Authorize()]
	public IActionResult Profile()
	{
		var currentUser = _currentUserService.GetCurrentUser();
		var profileImageUrl = string.Empty;

		// Check for profile image if exists
		if (currentUser.ProfileImage != null)
		{
			profileImageUrl = Path.Combine(_webHostEnvironment.WebRootPath, "img", "profile", currentUser.ProfileImage);
		}

		var imageFile = currentUser.ProfileImage;

		var model = new ProfileViewModel
		{
			UserId = currentUser.Id,
			FirstName = currentUser.FirstName,
			LastName = currentUser.LastName,
			Email = currentUser.Email,
			Designation = currentUser.Designation,
			ProfileImageUrl = System.IO.File.Exists(profileImageUrl) ? currentUser.ProfileImage : "no-image.png",
			HasProfileImage = !string.IsNullOrEmpty(currentUser.ProfileImage) && currentUser.ProfileImage != "no-image.png",
		};

		return View(model);
	}


	//[Route("account/accessdenied")]
	public IActionResult AccessDenied()
	{
		var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
		var currentUser = _context.Users.Include(f => f.UserCompany).Single(f => f.Id == userId);

		return LocalRedirect($"~/{currentUser.UserCompany.Id}/restricted/accessdenied");
	}

	#endregion


	#region "POST"

	[HttpPost("account/login")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(SignInViewModel model)
	{
		var companies = _context.Companies
			.Where(x => x.IsActive == true)
			.Select(x => new
			{
				x.Id,
				x.Name,
			})
			.ToList();

		ViewBag.CompanyList = companies;

		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = _context.Users
			.Include(e => e.UserCompany)
			.Include(e => e.UserRoles)
			.ThenInclude(e => e.Role)
			.FirstOrDefault(e => e.Email == model.Email);

		if (user == null)
		{
			ModelState.AddModelError("invalid_user", "Invalid user");
			return View(model);
		}

		if (!user.IsActive)
		{
			ModelState.AddModelError("inactive_user", "User is inactive. Contact system administrator.");
			return View(model);
		}

		// Skip company checking if user is in role SUPERUSER
		var isSuperUser = user.UserRoles.Any(e => e.Role.NormalizedName == "SUPERUSER");

		if (!isSuperUser)
		{
			if (user.UserCompany.Id.ToString() != model.CompanyId)
			{
				ModelState.AddModelError("invalid_company", "Invalid company");
				return View(model);
			}
		}

		var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

		if (result.Succeeded)
		{
			await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Information, "SIGN_IN", $"User ({user.UserName}) has successfully logged in.", user);

			if (!string.IsNullOrEmpty(model.Entity) && !string.IsNullOrEmpty(model.EntityId))
			{
				//return LocalRedirect($"/{model.CompanyId}/landing?entity={model.Entity}&entityId={model.EntityId}");
			}

			return LocalRedirect(WebUtility.UrlDecode($"/{model.CompanyId}/landing"));
		}

		if (result.IsLockedOut)
		{
			await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Error, "SIGN_IN", $"User ({user.UserName}) account has been locked out.", user);

			ModelState.AddModelError(string.Empty, "Your account has been locked out. Please try again in 10 minutes.");
			return View();
		}
		else
		{
			await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Error, "SIGN_IN", $"Invalid login attempt.", user);

			ModelState.AddModelError(string.Empty, "Invalid login attempt.");
			return View();
		}
	}


	[HttpPost("account/forgot")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = await _userManager.FindByNameAsync(model.Email);

		if (user == null)
		{
			await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Error, "FORGOT_PASSWORD", $"User not found.", null);

			model.ErrorMessage = "User not found. Please try another.";
			return View(model);
		}

		if (!user.IsActive)
		{
			await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Error, "FORGOT_PASSWORD", $"Account is not active.", user);

			model.ErrorMessage = "Your account is not active. Please contact the system administrator.";
			return View(model);
		}

		var token = await _userManager.GeneratePasswordResetTokenAsync(user);

		// Get email template
		var htmlTemplate = System.IO.File.ReadAllText(Path.Combine(_webHostEnvironment.WebRootPath, "templates\\html\\reset-password.html"));
		var template = Template.Parse(htmlTemplate);

		var templateData = new
		{
			RecipientName = $"{user.FirstName} {user.LastName}",
			ResetLink = $"https://{Environment.GetEnvironmentVariable("APP_DOMAIN")}/account/reset-password?token={Base64UrlEncoder.Encode(token)}&id={user.Id}&email={user.Email}",
		};

		var renderedHtml = template.Render(templateData);

		var bodyBuilder = new BodyBuilder();
		bodyBuilder.HtmlBody = renderedHtml;

		var message = new MimeMessage();
		message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
		message.To.Add(new MailboxAddress($"{user.FirstName} {user.LastName}", user.Email));
		message.Subject = "Request for password reset";
		message.Body = bodyBuilder.ToMessageBody();

		using var client = new SmtpClient();

		client.Connect(_emailSettings.Server, _emailSettings.Port, false);
		client.Authenticate(_emailSettings.UserName, _emailSettings.Password);

		client.Send(message);
		client.Disconnect(true);

		model.Email = string.Empty;
		model.ErrorMessage = string.Empty;
		model.EmailSent = true;
		model.Message = "Your reset password link has been successfully sent to your email.";

		await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Information, "FORGOT_PASSWORD", $"Reset password link has been successfully.", user);

		return View(model);
	}


	[HttpPost("account/reset-password")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = await _userManager.FindByIdAsync(model.Id);

		if (user == null)
		{
			await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Error, "RESET_PASSWORD", $"User not found.", null);

			model.ErrorMessage = "User not found. Please try again.";
			return View(model);
		}

		var _result = await _userManager.ResetPasswordAsync(user, Base64UrlEncoder.Decode(model.ResetToken), model.ConfirmPassword);

		if (!_result.Succeeded)
		{
			foreach (var error in _result.Errors)
			{
				ModelState.TryAddModelError(error.Code, error.Description);
			}

			await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Error, "RESET_PASSWORD", $"Reset password failed.", null);

			return View();
		}

		model.ResetOK = true;
		model.Message = "Your password has been reset successfully.";

		await _logService.LogMessageAsync(Core.Enums.LogTypeEnum.Error, "RESET_PASSWORD", $"Password has been reset successfully.", user);

		return View(model);
	}

	#endregion


	#region "PUT"

	[HttpPut("{company}/account/profile")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Profile(Guid company)
	{
		try
		{
			var req = Request.Form;

			if (req == null)
			{
				return BadRequest("Request is empty");
			}

			var user = await _userManager.FindByIdAsync(req["UserId"]);

			if (user == null)
			{
				return BadRequest("User not found");
			}

			// Update user's properties
			user.FirstName = req["FirstName"];
			user.LastName = req["LastName"];
			user.Designation = req["Designation"];
			user.Email = req["Email"];

			await _userManager.UpdateAsync(user);

			// Update user's email address
			await _userManager.UpdateNormalizedEmailAsync(user);

			return Ok();
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}


	[HttpPut("{company}/account/changepassword")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ChangePassword(Guid company)
	{
		try
		{
			var req = Request.Form;

			if (req == null)
			{
				return BadRequest(new
				{
					Message = "Request is empty",
				});
			}

			var user = await _userManager.FindByIdAsync(req["UserId"]);

			if (user == null)
			{
				return BadRequest(new
				{
					Message = "User not found",
				});
			}

			var result = await _userManager.ChangePasswordAsync(user, req["Current"], req["New"]);

			if (!result.Succeeded)
			{
				return BadRequest(new
				{
					Message = "Unable to process your request due to:",
					Errors = result.Errors.ToList(),
				});
			}

			return Ok();
		}
		catch (Exception ex)
		{
			return BadRequest(new
			{
				Message = ex.Message,
			});
		}
	}

	#endregion


	public async Task<ActionResult> UploadFile(IEnumerable<IFormFile> files)
	{
		var newFileName = string.Empty;

		// The Name of the Upload component is "files"
		if (files != null)
		{
			var currentUser = _currentUserService.GetCurrentUser();

			foreach (var file in files)
			{
				var fileContent = ContentDispositionHeaderValue.Parse(file.ContentDisposition);

				// Some browsers send file names with full path.
				// We are only interested in the file name.
				var fileName = Path.GetFileName(fileContent.FileName.ToString().Trim('"'));
				var extension = Path.GetExtension(fileContent.FileName.ToString().Trim('"'));
				newFileName = $"{Guid.NewGuid()}{extension}";

				var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "profile", newFileName);

				// The files are not actually saved in this demo
				using (var fileStream = new FileStream(physicalPath, FileMode.Create))
				{
					await file.CopyToAsync(fileStream);

					currentUser.ProfileImage = newFileName;
					await _userManager.UpdateAsync(currentUser);
				}
			}
		}

		return Ok(new
		{
			FileName = newFileName,
		});
	}


	[HttpPut("{company}/account/removeimage")]
	public async Task<IActionResult> RemoveImage(Guid company)
	{
		var currentUser = _currentUserService.GetCurrentUser();

		if (currentUser == null)
		{
			return BadRequest(new
			{
				Message = "User not found"
			});
		}

		if (!string.IsNullOrEmpty(currentUser.ProfileImage))
		{
			var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "profile", currentUser.ProfileImage);

			if (System.IO.File.Exists(fullPath))
			{
				System.IO.File.Delete(fullPath);
			}

			currentUser.ProfileImage = null;
			await _userManager.UpdateAsync(currentUser);

			return Ok(new
			{
				FileRemoved = true,
			});
		}

		return Ok(new
		{
			FileRemoved = false,
		});
	}


	#region "Private methods/functions"

	private UserInfo CreateUser()
	{
		try
		{
			return Activator.CreateInstance<UserInfo>();
		}
		catch
		{
			throw new InvalidOperationException($"Can't create an instance of '{nameof(UserInfo)}'. " +
				 $"Ensure that '{nameof(UserInfo)}' is not an abstract class and has a parameterless constructor, or alternatively " +
				 $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
		}
	}

	private IUserEmailStore<UserInfo> GetEmailStore()
	{
		if (!_userManager.SupportsUserEmail)
		{
			throw new NotSupportedException("The default UI requires a user store with email support.");
		}

		return (IUserEmailStore<UserInfo>)_userStore;
	}

	#endregion

}
