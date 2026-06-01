using PermitPro.Core.Entities;

namespace PermitPro.Core.Interfaces;

public interface ICurrentUserService
{
	Task<bool> IsContractor();

	Task<bool> IsPermitIssuer();

	Task<bool> IsLeadPermitIssuer();

	Task<UserInfo> GetCurrentUserAsync();

	Task<IList<string>> GetCurrentUserRoles();

	UserInfo GetCurrentUser();
}
