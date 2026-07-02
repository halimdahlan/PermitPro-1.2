using PermitPro.Core.Data;

using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class ManageRoleViewModel
{
	public string? Id { get; set; }

	[Required(ErrorMessage = "Role name is required")]
	public required string Name { get; set; }

	[Required(ErrorMessage = "Normalized name is required")]
	[RegularExpression(@"^[A-Z]+$", ErrorMessage = "Normalized name must be uppercase letters only (no spaces, numbers, or special characters).")]
	public required string NormalizedName { get; set; }

	public string? Description { get; set; }

	public bool IsSystemRole { get; set; }

	public bool IsUnlimitedUsers { get; set; }

	public bool IsEdit { get; set; }

	public List<string>? UsersInRole { get; set; } = new();
}

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
