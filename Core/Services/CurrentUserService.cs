#nullable disable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

using System.Security.Claims;

namespace PermitPro.Core.Services;

public class CurrentUserService : ICurrentUserService
{
	private readonly IHttpContextAccessor _dbContextAccessor;
	private readonly UserManager<UserInfo> _userManager;
	private readonly ApplicationDbContext _dbContext;

	public CurrentUserService(IHttpContextAccessor contextAccessor, UserManager<UserInfo> userManager, ApplicationDbContext context)
	{
		_dbContextAccessor = contextAccessor;
		_dbContext = context;
		_userManager = userManager;
	}

	public async Task<bool> IsContractor()
	{
		var userRoles = await GetCurrentUserRoles();
		return userRoles.Contains("Contractor") || userRoles.Contains("Permit Holder / Contractor");
	}


	public async Task<bool> IsPermitIssuer()
	{
		var userRoles = await GetCurrentUserRoles();
		return userRoles.Contains("Permit Issuer");
	}


	public async Task<bool> IsLeadPermitIssuer()
	{
		var userRoles = await GetCurrentUserRoles();
		return userRoles.Contains("Lead Permit Issuer");
	}


	public async Task<bool> IsAGT()
	{
		var userRoles = await GetCurrentUserRoles();
		return userRoles.Contains("Authorized Gas Tester");
	}


	public async Task<UserInfo> GetCurrentUserAsync()
	{
		var userId = _dbContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
		return await _dbContext.Users
			.Include(f => f.UserCompany)
			.Include(f => f.UserRoles)
			.ThenInclude(f => f.Role)
			.FirstOrDefaultAsync(f => f.Id == userId);
	}


	public async Task<IList<string>> GetCurrentUserRoles()
	{
		var user = await GetCurrentUserAsync();
		return await _userManager.GetRolesAsync(user);
	}


	public UserInfo GetCurrentUser()
	{
		var userId = _dbContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
		return _dbContext.Users
			.Include(f => f.UserCompany)
			.Include(f => f.UserRoles)
			.ThenInclude(f => f.Role)
			.FirstOrDefault(f => f.Id == userId);
	}
}
