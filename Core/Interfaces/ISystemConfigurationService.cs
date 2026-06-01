
using PermitPro.Core.Entities;

namespace PermitPro.Core.Interfaces;

public interface ISystemConfigurationService
{
	IEnumerable<SystemMenu> AuthorizedMenus { get; }

	IEnumerable<string> ReservedRoles { get; }

	int UserCreateLimit { get; }

	void Init();

	bool HasAccess(string controller);

}
