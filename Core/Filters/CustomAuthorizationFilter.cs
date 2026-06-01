using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PermitPro.Core.Filters;

public class CustomAuthorizationFilter : IAuthorizationFilter
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	public CustomAuthorizationFilter(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	public void OnAuthorization(AuthorizationFilterContext context)
	{
		throw new NotImplementedException();
	}
}
