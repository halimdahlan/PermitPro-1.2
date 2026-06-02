using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class SignInViewModel
{
	[Required(ErrorMessage = "This field is required")]
	[EmailAddress(ErrorMessage = "Enter a valid email address")]
	[Display(Name = "Email address")]
	public required string Email { get; set; }

	[Required(ErrorMessage = "This field is required")]
	[MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
	[DataType(DataType.Password)]
	[Display(Name = "Password")]
	public required string Password { get; set; }

	[Display(Name = "Remember me")]
	public bool RememberMe { get; set; }

	[Required(ErrorMessage = "This field is required")]
	[Display(Name = "Company")]
	public required string CompanyId { get; set; }

	public string? Token { get; set; }

	public string? ReturnUrl { get; set; }

	public string? Entity { get; set; }

	public string? EntityId { get; set; }

	public string? Origin { get; set; }
}
