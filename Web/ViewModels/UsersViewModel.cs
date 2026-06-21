using PermitPro.Core.Data;

namespace PermitPro.App.ViewModels;

public class UsersViewModel : BaseViewModel
{
	public IEnumerable<Role>? Roles { get; set; }

	public IEnumerable<UserRolesInfo>? UserRolesInfos { get; set; }

	public bool LimitReached { get; set; }
}


public class UserRolesInfo
{
	public required string Id { get; set; }
	public required string Name { get; set; }
	public int NumOfUsers { get; set; }
	public bool IsSystemRole { get; set; }
	public DateTime CreatedWhen { get; set; }
}
