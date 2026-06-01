using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using PermitPro.Core.Data;
using System.Security.Claims;

namespace PermitPro.Core.Filters;

public class RoleBasedAuth : IAuthorizationFilter
{
	private readonly string _controller;
	private readonly ApplicationDbContext _dbContext;
	private readonly IHttpContextAccessor _httpContextAccessor;

	public RoleBasedAuth(ApplicationDbContext context, string controller, IHttpContextAccessor httpContextAccessor)
	{
		_controller = controller;
		_dbContext = context;
		_httpContextAccessor = httpContextAccessor;
	}


	public void OnAuthorization(AuthorizationFilterContext context)
	{
		var userId = _httpContextAccessor!.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (userId == null)
		{
			_httpContextAccessor.HttpContext.Response.Redirect("/account/login");
		}

		//context.Result = new JsonResult("Permission denied");
	}
}
