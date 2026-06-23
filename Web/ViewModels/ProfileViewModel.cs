using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class ProfileMainViewModel
{
	public ProfileViewModel? ProfileForm { get; set; }
	public ProfilePasswordViewModel? ProfilePasswordForm { get; set; }
}

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


public class ProfilePasswordViewModel
{
	public string? UserId { get; set; }
	
	[DataType(DataType.Password)]
	[Required(ErrorMessage = "This field is required")]
	[MinLength(8, ErrorMessage = "Mininum length is 8 characters")]
	public required string CurrentPassword { get; set; }

	[DataType(DataType.Password)]
	[Required(ErrorMessage = "This field is required")]
	[MinLength(8, ErrorMessage = "Mininum length is 8 characters")]
	public required string Password { get; set; }

	[DataType(DataType.Password)]
	[Required(ErrorMessage = "This field is required")]
	[Compare("Password", ErrorMessage = "Both passwords do not match")]
	[MinLength(8, ErrorMessage = "Mininum length is 8 characters")]
	public required string ConfirmPassword { get; set; }

	public string? ProfileImageUrl { get; set; }
}