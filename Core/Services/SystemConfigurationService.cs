#nullable disable

using DocumentFormat.OpenXml.Drawing.Charts;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;

using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

using System.Security.Claims;

namespace PermitPro.Core.Services;

public class SystemConfigurationService : ISystemConfigurationService
{
	private readonly UserManager<UserInfo> _userManager;
	private readonly ApplicationDbContext _context;
	private readonly ICurrentUserService _currentUserService;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly IWebHostEnvironment _webHostEnvironment;

	private List<SystemMenu> _authorizedMenus = new();


	public SystemConfigurationService(
		UserManager<UserInfo> userManager
		, ApplicationDbContext dbContext
		, ICurrentUserService currentUserService
		, IHttpContextAccessor httpContextAccessor
		, IWebHostEnvironment webHostEnvironment)
	{
		_userManager = userManager;
		_context = dbContext;
		_currentUserService = currentUserService;
		_httpContextAccessor = httpContextAccessor;
		_webHostEnvironment = webHostEnvironment;
	}


	public IEnumerable<SystemMenu> AuthorizedMenus
	{
		get
		{
			var sessionMenu = _httpContextAccessor.HttpContext.Session.GetString("AuthorizedMenu");

			if (string.IsNullOrEmpty(sessionMenu))
			{
				Init();
			}
			else
			{
				_authorizedMenus = JsonConvert.DeserializeObject<List<SystemMenu>>(sessionMenu);
			}

			return _authorizedMenus.AsEnumerable();
		}
	}


	public int UserCreateLimit
	{
		get
		{
			return Convert.ToInt16(Environment.GetEnvironmentVariable("USER_CREATE_LIMIT"));
		}
	}


	public IEnumerable<string> ReservedRoles
	{
		get
		{
			string reservedRoles = Environment.GetEnvironmentVariable("RESERVED_ROLES");

			if (!string.IsNullOrEmpty(reservedRoles))
			{
				return reservedRoles.Split(';');
			}

			return null;
		}
	}


	public void Init()
	{
		var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (!string.IsNullOrEmpty(userId))
		{
			var currentUser = _context.Users
				.Include(e => e.UserRoles)
				.FirstOrDefault(e => e.Id == userId);

			var currentUserRoles = _context.UserRoles
				.Include(e => e.Role)
				.Where(e => e.User == currentUser);

			List<string> roles = new();

			foreach (var role in currentUserRoles)
			{
				roles.Add(role.Role.Name);
			}

			_httpContextAccessor.HttpContext.Session.SetString("UserRoles", JsonConvert.SerializeObject(roles));

			var menu = _context.SystemMenus
				.Include(e => e.Roles)
				.Where(e => e.Roles!.Any(f => roles.Contains(f.Name)) && e.ParentId == null)
				.OrderBy(e => e.MenuOrder)
				.Select(e => new SystemMenu
				{
					Id = e.Id,
					Name = e.Name,
					Description = e.Description,
					ControllerName = e.ControllerName,
					IconName = e.IconName,
					ParentId = e.ParentId,
					HasChildren = _context.SystemMenus.Include(f => f.Roles).Any(f => f.ParentId == e.Id),
					Children = _context.SystemMenus.Include(f => f.Roles)
						.Where(f => f.ParentId == e.Id)
						.OrderBy(f => f.MenuOrder)
						.Select(f => new SystemMenu
						{
							Id = f.Id,
							Name = f.Name,
							Description = f.Description,
							ControllerName = f.ControllerName,
							IconName = f.IconName,
						}).ToList(),
					MenuOrder = e.MenuOrder,
					CreatedBy = e.CreatedBy,
				})
				.ToList();

			_authorizedMenus = menu;

			_httpContextAccessor.HttpContext.Session.SetString("AuthorizedMenu", JsonConvert.SerializeObject(menu));
		}
	}


	public bool HasAccess(string controller)
	{
		var sessionRoles = _httpContextAccessor.HttpContext.Session.GetString("UserRoles");

		if (string.IsNullOrEmpty(sessionRoles))
		{
			return false;
		}

		var userRoles = JsonConvert.DeserializeObject<List<string>>(sessionRoles);

		var menu = _context.SystemMenus
			.Include(e => e.Roles)
			.FirstOrDefault(e => e.Roles!.Any(f => userRoles.Contains(f.Name)) && e.ControllerName.ToLower() == controller.ToLower());

		return menu != null;
	}

}
