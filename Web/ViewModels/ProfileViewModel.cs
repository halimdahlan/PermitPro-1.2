using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class ProfileViewModel
{
	public string? UserId { get; set; }

	[Required(ErrorMessage = "First name is required.")]
	public string? FirstName { get; set; }

	[Required(ErrorMessage = "Last name is required.")]
	public string? LastName { get; set; }

	[EmailAddress(ErrorMessage = "Invalid email address format.")]
	public string? Email { get; set; }

	public string? Designation { get; set; }

	[Phone]
	public string? PhoneNumber { get; set; }

	public string? ChangeEmailToken { get; set; }

	public string? ProfileImageUrl { get; set; }

	public bool HasProfileImage { get; set; }

	public List<string> RoleNames { get; set; } = new();

	public bool IsSuperAdmin { get; set; }

}
