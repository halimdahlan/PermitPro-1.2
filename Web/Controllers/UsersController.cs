#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PermitPro.App.Controllers.Base;
using PermitPro.App.Models.Ajax;
using PermitPro.App.ViewModels;
using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Enums;
using PermitPro.Core.Helpers;
using PermitPro.Core.Interfaces;

using System.Security.Claims;
using System.Text.Json;

namespace PermitPro.App.Controllers;

[Authorize]
public class UsersController : AppControllerBase
{
	private readonly ApplicationDbContext _dbContext;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly SignInManager<UserInfo> _signInManager;
	private readonly ISystemConfigurationService _systemConfiguration;
	private readonly UserManager<UserInfo> _userManager;
	private readonly RoleManager<Role> _roleManager;
	private readonly IWebHostEnvironment _webHostEnvironment;

	//private readonly JsonSerializerOptions _jsonOptions;

	public UsersController(
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

	[HttpGet("{company}/users")]
	public IActionResult Index(Guid company)
	{
		var userRoles = _dbContext.Roles
			.Where(e => e.NormalizedName != "SUPERUSER")
			.Select(e => new
			{
				e.Id,
				e.Name,
			})
			.ToList();

		userRoles.Insert(0, new { Id = string.Empty, Name = "(select a role)" });
		ViewBag.UserRoles = userRoles;

		var sites = _dbContext.Sites
			.Include(e => e.SiteCompany)
			.Where(e => e.IsActive == true && e.SiteType == SiteTypeEnum.Site && e.SiteCompany.Id == company);

		var locations = sites
			.Select(e => new
			{
				Id = e.Id.ToString(),
				e.Name,
			})
			.ToList();

		locations.Insert(0, new { Id = string.Empty, Name = "All" });
		ViewBag.Locations = locations;

		var totalUsers = _dbContext.Users.Include(e => e.UserCompany).Count(e => e.UserCompany.Id == company);

		var model = new UsersViewModel
		{
			CompanyId = company,
			Roles = _dbContext.Roles.Where(role => role.NormalizedName != "SUPERUSER").ToList(),
			LimitReached = totalUsers > _systemConfiguration.UserCreateLimit,
		};


		return View(model);
	}


	[HttpGet("{company}/users/new")]
	public IActionResult New(Guid company, string org)
	{
		var model = new NewUserViewModel
		{
			FirstName = string.Empty,
			LastName = string.Empty,
			Email = string.Empty,
			UserRole = string.Empty,
			CompanyID = company
		};

		var userRoles = _dbContext.Roles
			.Where(e => e.NormalizedName != "SUPERUSER")
			.Select(e => new RoleDropDown
			{
				Id = e.Id,
				Name = e.Name,
			})
			.ToList();

		var totalUsers = _dbContext.Users.Include(e => e.UserCompany).Count(e => e.UserCompany.Id == company);

		model.Roles = userRoles;
		model.MaxNumOfUsers = _systemConfiguration.UserCreateLimit;
		model.HasExceededLimit = false; //totalUsers > _systemConfiguration.UserCreateLimit;
		model.OriginFromContractors = !string.IsNullOrEmpty(org) && org == "c";

		return View(model);
	}


	[HttpPost("{company}/users/new")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> New(Guid company, NewUserViewModel model)
	{
		if (!ModelState.IsValid)
			return View(model);

		var userCompany = await _dbContext.Companies.FirstOrDefaultAsync(e => e.Id == company);

		var userInfo = new UserInfo
		{
			Id = Guid.NewGuid().ToString(),
			FirstName = model.FirstName,
			LastName = model.LastName,
			Email = model.Email,
			Designation = model.Designation,
			UserName = model.Email,
			NormalizedUserName = model.Email.ToUpper(),
			NormalizedEmail = model.Email.ToUpper(),
			EmailConfirmed = true,
			IsActive = true,
			UserCompany = userCompany,
			CreatedBy = Guid.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)),
			CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
		};

		_dbContext.Users.Add(userInfo);
		await _dbContext.SaveChangesAsync();

		// Add user to the selected role
		var role = _roleManager.Roles.FirstOrDefault(e => e.Id == model.UserRole);
		await _userManager.AddToRoleAsync(userInfo, role.NormalizedName);

		// Add user to selected site(s)
		if (!string.IsNullOrEmpty(model.Locations))
		{
			var locations = JsonSerializer.Deserialize<List<string>>(model.Locations);

			foreach (var location in locations)
			{
				var loc = _dbContext.Sites.FirstOrDefault(e => e.Id == Guid.Parse(location));
				userInfo.Sites.Add(loc);
			}
		}

		await _dbContext.SaveChangesAsync();

		// Set user password if any
		userInfo.PasswordHash = _userManager.PasswordHasher.HashPassword(userInfo, model.ConfirmPassword);

		await _userManager.UpdateAsync(userInfo);
		await _userManager.CheckPasswordAsync(userInfo, model.ConfirmPassword);

		TempData["SuccessMessage"] = $"User ({userInfo.Email}) has been successfully created.";

		if (model.OriginFromContractors)
			return Redirect($"/{company}/contractors");

		return RedirectToAction("Index", new { company });
	}


	[HttpGet("{company}/users/edit/{id}")]
	public async Task<IActionResult> Edit(Guid company, Guid id, string org)
	{
		var user = await _dbContext.Users
			.Include(e => e.UserRoles)
			.Include(e => e.Sites)
			.Select(e => new ManageUserViewNodel
			{
				Id = e.Id,
				FirstName = e.FirstName,
				LastName = e.LastName,
				Email = e.Email,
				Designation = e.Designation,
				UserRole = e.UserRoles.FirstOrDefault().RoleId ?? string.Empty,
				CompanyID = company,
				Locations = JsonSerializer.Serialize(e.Sites.Select(s => s.Id).ToList()),
				IsActive = e.IsActive
			})
			.FirstOrDefaultAsync(e => e.Id == id.ToString());

		if (user == null)
			return NotFound("User not found!");

		var userRoles = _dbContext.Roles
			.Where(e => e.NormalizedName != "SUPERUSER")
			.Select(e => new RoleDropDown
			{
				Id = e.Id,
				Name = e.Name,
			})
			.ToList();
		
		user.Roles = userRoles;
		user.IsEdit = true;
		user.OriginFromContractors = !string.IsNullOrEmpty(org) ? org == "c" : false;

		var model = new ManageUserMainViewModel
		{
			UserInfoForm = user,
			UserPasswordForm = new ManageUserPasswordViewModel
			{
				NewPassword = string.Empty,
				ConfirmPassword = string.Empty,
				IsContractors = !string.IsNullOrEmpty(org) && org == "c"
      }
		};

		return View("Edit", model);
	}


	[HttpPost("{company}/users/edit/{id}")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(Guid company, Guid id, ManageUserMainViewModel model)
	{
		ModelState.Remove("UserPasswordForm");

		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var userInfo = await _dbContext.Users
			.Include(e => e.UserRoles)
			.ThenInclude(e => e.Role)
			.Include(e => e.Sites)
			.FirstOrDefaultAsync(e => e.Id == id.ToString());

		if (userInfo == null)
			return NotFound("User not found!");

		var oldUserRole = userInfo.UserRoles.FirstOrDefault(e => e.User == userInfo);

		userInfo.FirstName = model.UserInfoForm.FirstName;
		userInfo.LastName = model.UserInfoForm.LastName;
		userInfo.Designation = model.UserInfoForm.Designation;
		userInfo.IsActive = model.UserInfoForm.IsActive;
		userInfo.UpdatedWhen = DateTime.UtcNow.ToUniversalTime();
		userInfo.UpdatedBy = Guid.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier));

		await _userManager.UpdateAsync(userInfo);

		// Update user's role
		await _userManager.RemoveFromRoleAsync(userInfo, oldUserRole.Role.NormalizedName);

		var role = _roleManager.Roles.FirstOrDefault(e => e.Id == model.UserInfoForm.UserRole);
		await _userManager.AddToRoleAsync(userInfo, role.NormalizedName);

		// Update user's site(s)
		var sitesToRemove = userInfo.Sites.ToList();

		sitesToRemove.ForEach(site =>
		{
			userInfo.Sites.Remove(site);
		});

		if (!string.IsNullOrEmpty(model.UserInfoForm.Locations))
		{
			var locations = JsonSerializer.Deserialize<List<string>>(model.UserInfoForm.Locations);
			var hasLocations = locations.Any();
			var locationCount = locations.Count();

			if (locations.Any() && locations.Count() > 0)
			{
				foreach (var location in locations)
				{
					var loc = _dbContext.Sites.FirstOrDefault(e => e.Id == Guid.Parse(location));
					userInfo.Sites.Add(loc);
				}
			}
		}
		
		await _dbContext.SaveChangesAsync();

		TempData["SuccessMessage"] = "User information has been successfully updated.";

		if (model.UserInfoForm.OriginFromContractors)
			return Redirect($"/{company}/contractors");

		return RedirectToAction("Index", new { company });
	}


	[HttpPost("{company}/users/setpassword/{id}/setpassword")]
	[ValidateAntiForgeryToken]
	public IActionResult SetPassword(Guid company, Guid id, ManageUserMainViewModel model)
	{
		ModelState.Remove("UserInfoForm");

		if (!ModelState.IsValid)
		{
			return View(model);
		}

		return RedirectToAction("Edit", new { company, id });
	}


	public IActionResult UserRoles()
	{
		var roles = _dbContext.Roles
			.Include(e => e.UserRoles)
			.ThenInclude(e => e.User)
			.Select(e => new UserRolesInfo
			{
				Id = e.Id,
				Name = e.Name,
				NumOfUsers = e.UserRoles.Count(),
				IsNonEditable = CheckIfRoleIsNonEditable(e.NormalizedName),
			})
			.OrderBy(e => e.Name)
			.ToList();

		var model = new UsersViewModel
		{
			UserRolesInfos = roles
		};

		return View(model);
	}


	[HttpGet("{company}/users/grid")]
	public JsonResult GetUsersGrid(Guid company)
	{
		var siteUsers = _dbContext.Users
			.Include(e => e.UserCompany)
			.Include(e => e.UserRoles)
			.ThenInclude(e => e.Role)
			.Where(e => e.UserRoles.Any(e => e.Role.NormalizedName != "SUPERUSER") && e.UserCompany.Id == company)
			.OrderByDescending(e => e.CreatedWhen)
			.Select(e => new UsersGridViewModel
			{
				Id = e.Id,
				Name = string.Format("{0} {1}", e.FirstName, e.LastName).Trim(),
				Email = e.Email,
				IsSecured = e.PasswordHash != null,
				IsActive = e.IsActive,
				Sites = string.Join(", ", _dbContext.Sites.Include(site => site.Users).Where(site => site.Users.Select(user => user.Id).Contains(e.Id)).Select(x => x.Name).ToList()),
				Roles = string.Join(", ", e.UserRoles.Select(role => role.Role.Name).ToList()),
				Designation = e.Designation,
				CreatedWhen = GeneralHelper.GetDateInTimeZone(e.CreatedWhen),
				ActionIcons = UsersGridActionIcons(company, e.Id, e.PasswordHash != null)
			})
			.ToList();

		return new JsonResult(siteUsers, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}


	[HttpGet("{company}/users/filters")]
	public JsonResult GetUserFilters(Guid company)
	{
		var filters = new List<dynamic>
		{
			new { text = "All", value = "all|all" },
			new { text = "Active", value = "isActive|true" },
			new { text = "Inactive", value = "isActive|false" }
		};

		var roles = _dbContext.Roles
			.Where(e => e.NormalizedName != "SUPERUSER")
			.OrderBy(e => e.Name)
			.Select(e => new { text = e.Name, value = $"roles|{e.Name}" })
			.ToList();

		filters.AddRange(roles);

		return new JsonResult(filters, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
		});
	}


	[HttpGet("{company}/users/workflowusers/all")]
	public ActionResult<IEnumerable<object>> GetWorkflowUsers(Guid company)
	{
		var users = _dbContext.Users
			.Include(e => e.UserCompany)
			.Include(e => e.UserRoles)
			.ThenInclude(e => e.Role)
			.Where(e => e.UserRoles.Any(f => f.Role.NormalizedName == "PERMITISSUER" || f.Role.NormalizedName == "LEADPERMITISSUER" || f.Role.NormalizedName == "PORTALADMIN") && e.IsActive && e.UserCompany.Id == company)
			.OrderBy(e => e.FirstName)
			.Select(e => new
			{
				e.Id,
				e.FirstName,
				e.LastName,
				e.Email,
			})
			.ToList();

		return Ok(new
		{
			Data = users
		});
	}


	[HttpGet("{company}/users/{searchBy?}/value/{searchValue?}")]
	public ActionResult GetUsers(Guid company, string searchBy = "all", string searchValue = null)
	{
		var siteUsers = _dbContext.Users
			.Include(e => e.UserCompany)
			.Include(e => e.UserRoles)
			.ThenInclude(e => e.Role)
			.Where(e => e.UserRoles.Any(e => e.Role.NormalizedName != "SUPERUSER") && e.UserCompany.Id == company)
			.ToList();

		if (searchBy != "all")
		{
			if (searchBy == "isActive")
			{
				siteUsers = siteUsers.Where(e => e.IsActive == Convert.ToBoolean(searchValue)).ToList();
			}
			else
			{
				siteUsers = siteUsers.Where(e => e.UserRoles.Any(e => e.RoleId == searchValue)).ToList();
			}
		}

		var users = siteUsers
			.OrderByDescending(e => e.CreatedWhen)
			.Select(e => new
			{
				e.Id,
				Name = string.Format("{0} {1}", e.FirstName, e.LastName).Trim(),
				e.Email,
				IsSecured = e.PasswordHash != null,
				e.IsActive,
				Sites = string.Join(", ", _dbContext.Sites.Include(site => site.Users).Where(site => site.Users.Select(user => user.Id).Contains(e.Id)).Select(x => x.Name).ToList()),
				Roles = string.Join(", ", e.UserRoles.Select(role => role.Role.Name).ToList()),
				e.Designation,
				CreatedWhen = GeneralHelper.GetDateInTimeZone(e.CreatedWhen).ToString("dd MMM, yyyy"),
				UpdatedWhen = e.UpdatedWhen?.ToLocalTime().ToString("dd MMM, yyyy"),
				CreatedWhenTicks = GeneralHelper.FormatDateTimeTicks(GeneralHelper.GetDateInTimeZone(e.CreatedWhen)),
				UpdatedWhenTicks = GeneralHelper.FormatDateTimeTicks(e.UpdatedWhen),
			})
			.ToList();

		return Ok(new
		{
			Data = users
		});
	}


	[HttpGet("{company}/users/{id}/info")]
	public async Task<IActionResult> GetUser(string id)
	{
		try
		{
			var user = await _dbContext.Users
				.Include(e => e.UserRoles)
				.ThenInclude(e => e.Role)
				.FirstOrDefaultAsync(e => e.Id == id);

			if (user == null)
			{
				return NotFound();
			}

			return Ok(new
			{
				User = new
				{
					user.Id,
					user.FirstName,
					user.LastName,
					user.Designation,
					user.Email,
					user.IsActive,
					IsSecured = user.PasswordHash != null,
					Roles = user.UserRoles.Select(e => new { e.RoleId, e.Role.Name }).ToList(),
					Sites = _dbContext.Sites.Include(site => site.Users)
						.Where(site => site.Users.Select(user => user.Id).Contains(user.Id))
						.Select(e => new { e.Id, e.Name }).ToList(),
				}
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}


	[HttpGet("users/roles/{id?}")]
	public async Task<IActionResult> GetRoleById(string id)
	{
		var role = await _dbContext.Roles.FirstOrDefaultAsync(e => e.Id == id);

		if (role == null)
		{
			return NotFound();
		}

		return Ok(new
		{
			role.Name,
			Description = ""
		});
	}


	[HttpPost("{company}/users/{mode}")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ManageUser(Guid company, string mode = "new")
	{
		try
		{
			var req = Request.Form;
			var companyInfo = _dbContext.Companies.SingleOrDefault(f => f.Id == company);

			if (mode == "new")
			{
				var userInfo = new UserInfo
				{
					Id = Guid.NewGuid().ToString(),
					FirstName = req["FirstName"].ToString(),
					LastName = req["LastName"].ToString(),
					Email = req["Email"].ToString(),
					IsActive = Convert.ToBoolean(req["IsActive"]),
					UserName = req["Email"].ToString(),
					Designation = req["Designation"].ToString(),
					NormalizedUserName = req["Email"].ToString().ToUpper(),
					NormalizedEmail = req["Email"].ToString().ToUpper(),
					EmailConfirmed = true,
					UserCompany = companyInfo,
					CreatedBy = Guid.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)),
					CreatedWhen = DateTime.UtcNow.ToUniversalTime(),
				};

				_dbContext.Users.Add(userInfo);
				_dbContext.SaveChanges();

				// Add user to selected role
				var role = _roleManager.Roles.FirstOrDefault(e => e.Id == req["Role"].ToString());
				await _userManager.AddToRoleAsync(userInfo, role.NormalizedName);

				// Add user to selected site(s)
				if (!string.IsNullOrEmpty(req["Locations"].ToString()))
				{
					var locations = req["Locations"].ToString().Split(',');

					foreach (var location in locations)
					{
						var loc = _dbContext.Sites.FirstOrDefault(e => e.Id == Guid.Parse(location));
						userInfo.Sites.Add(loc);
					}
				}

				_dbContext.SaveChanges();

				// Set user password if any
				if (!string.IsNullOrEmpty(req["Password"].ToString()) && req["Password"].ToString() != "")
				{
					userInfo.PasswordHash = _userManager.PasswordHasher.HashPassword(userInfo, req["Password"].ToString());
					await _userManager.UpdateAsync(userInfo);

					await _userManager.CheckPasswordAsync(userInfo, req["Password"].ToString());
				}

			}

			if (mode == "edit")
			{
				var userInfo = await _dbContext.Users
					.Include(e => e.UserRoles)
					.ThenInclude(e => e.Role)
					.Include(e => e.Sites)
					.FirstOrDefaultAsync(e => e.Id == req["Id"].ToString());

				if (userInfo == null)
				{
					return NotFound("Unable to find user");
				}

				var oldUserRole = userInfo.UserRoles.FirstOrDefault(e => e.User == userInfo);

				userInfo.FirstName = req["FirstName"].ToString();
				userInfo.LastName = req["LastName"].ToString();
				userInfo.Email = req["Email"].ToString();
				userInfo.Designation = req["Designation"].ToString();
				userInfo.NormalizedEmail = req["Email"].ToString().ToUpper();
				userInfo.IsActive = Convert.ToBoolean(req["IsActive"]);
				userInfo.UpdatedWhen = DateTime.UtcNow.ToUniversalTime();
				userInfo.UpdatedBy = Guid.Parse(_httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier));

				await _userManager.UpdateAsync(userInfo);

				// Update user's role
				await _userManager.RemoveFromRoleAsync(userInfo, oldUserRole.Role.NormalizedName);

				var role = _roleManager.Roles.FirstOrDefault(e => e.Id == req["Role"].ToString());
				await _userManager.AddToRoleAsync(userInfo, role.NormalizedName);


				// Update user's site(s)
				var sitesToRemove = userInfo.Sites.ToList();

				sitesToRemove.ForEach(site =>
				{
					userInfo.Sites.Remove(site);
				});

				if (!string.IsNullOrEmpty(req["Locations"].ToString()))
				{
					var locations = req["Locations"].ToString().Split(',');

					foreach (var location in locations)
					{
						var loc = _dbContext.Sites.FirstOrDefault(e => e.Id == Guid.Parse(location));
						userInfo.Sites.Add(loc);
					}
				}

				_dbContext.SaveChanges();
			}

			return Ok(new
			{
				Data = "OK"
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}

	}


	[HttpPost("{company}/users/{id}/setpasswordX")]
	public async Task<IActionResult> SetUserPassword(Guid company, [FromBody] AjaxUserPasswordModel request, string id)
	{
		try
		{
			var user = await _userManager.FindByIdAsync(id);

			if (user == null)
			{
				return NotFound("Unable to find user");
			}

			//var isPasswordOk = await _userManager.CheckPasswordAsync(user, request.Password);

			//if (!isPasswordOk)
			//{
			//	return Ok(new
			//	{
			//		Data = "NOT_OK",
			//	});
			//}

			user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password);
			await _userManager.UpdateAsync(user);

			//var result = await _userManager.ChangePasswordAsync(user, "", request.Password);

			return Ok(new
			{
				Data = "OK"
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}


	[HttpPost("users/roles/{mode?}")]
	public async Task<IActionResult> ManageRole(string mode = "new")
	{
		try
		{
			var req = Request.Form;

			if (mode == "new")
			{
				var normalizedName = req["Name"].ToString().ToUpper();
				var existingRole = await _dbContext.Roles.FirstOrDefaultAsync(e => e.NormalizedName == normalizedName);

				if (existingRole == null)
				{
					var role = new Role
					{
						Name = req["Name"].ToString(),
						ConcurrencyStamp = Guid.NewGuid().ToString(),
					};

					await _roleManager.CreateAsync(role);

					return Ok(new
					{
						Message = "Successfully created role."
					});
				}
				else
				{
					return BadRequest(new
					{
						ErrorMessage = "Found existing role. Unable to proceed."
					});
				}
			}
			else
			{
				var role = await _roleManager.FindByIdAsync(req["Id"]);

				if (role == null)
				{
					return NotFound();
				}

				role.Name = req["Name"].ToString();
				await _roleManager.UpdateAsync(role);

				return Ok(new
				{
					Message = "Successfully updated role."
				});
			}
		}
		catch (Exception ex)
		{
			return BadRequest(new
			{
				ErrorMessage = ex.Message
			});
		}
	}


	[HttpDelete("{company}/users/{id}")]
	public async Task<IActionResult> DeleteUser(Guid company, string id)
	{
		try
		{
			var user = await _userManager.FindByIdAsync(id);

			if (user == null)
			{
				return NotFound();
			}

			await _userManager.DeleteAsync(user);

			return Ok(new
			{
				Data = "OK"
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
	}


	#region "Private static functions"

	private static string UsersGridActionIcons(Guid company, string id, bool isSecured)
	{
		var icons = string.Empty;

		icons += "<div class=\"d-flex flex-row action-icons\">";
		icons += $"<a href=\"/{company}/users/edit/{id}\" class=\"no-loading text-secondary\"><i class=\"fa-solid fa-money-check-pen fa-lg\"></i></a>";
		icons += $"<a href=\"javascript:;\" class=\"no-loading text-danger\" onclick=\"deleteUser('{id}')\"><i class=\"fa-solid fa-trash-xmark fa-lg\"></i></a>";
		
		// icons += "<a href=\"javascript:;\" class=\"no-loading text-secondary menu-context dropdown-toggle\" data-bs-toggle=\"dropdown\" aria-expanded=\"false\"><i class=\"fa-solid fa-gear fa-lg\"></i></a>";
		// icons += "<ul class=\"dropdown-menu\">";

		// //if (isSecured)
		// //{
		// //	icons += "<li class=\"dropdown-item disabled\">";
		// //	icons += "Set password";
		// //}
		// //else
		// //{
		// //}

		// icons += "<li class=\"dropdown-item\">";
		// icons += $"<a href=\"javascript:;\" class=\"no-loading text-info\" onclick=\"setPassword(this)\" data-user-id=\"{id}\">Set password</a>";

		// icons += "</li>";
		// icons += "</ul>";

		icons += "</div>";

		return icons;
	}


	private static bool CheckIfRoleIsNonEditable(string roleName)
	{
		IEnumerable<string> reservedRoles = Environment.GetEnvironmentVariable("RESERVED_ROLES").Split(';');
		return reservedRoles.Contains(roleName);
	}

	#endregion

}