namespace PermitPro.App.ViewModels;

public class ProfileViewModel
{
	public string? UserId { get; set; }

	public string? FirstName { get; set; }

	public string? LastName { get; set; }

	public string? Email { get; set; }

	public string? Designation { get; set; }

	public string? ChangeEmailToken { get; set; }

	public string? ProfileImageUrl { get; set; }

	public bool HasProfileImage { get; set; }

}
