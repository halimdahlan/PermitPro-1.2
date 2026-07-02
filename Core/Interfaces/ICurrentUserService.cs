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

	/// <summary>
	/// Returns the company ID for the current request.
	/// For normal users this is their assigned UserCompany.
	/// For super admins (no company) this falls back to the {company} route value.
	/// </summary>
	Guid GetCurrentCompanyId();
}
