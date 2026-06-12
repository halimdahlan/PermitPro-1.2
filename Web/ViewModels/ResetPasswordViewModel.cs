using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class ResetPasswordViewModel
{
	[DataType(DataType.Password)]
	[StringLength(20, MinimumLength = 6, ErrorMessage = "Enter characters between 6 to 20 in length")]
	[Required(ErrorMessage = "Please enter your password")]
	[Display(Name = "Password")]
	public required string Password { get; set; }

	[DataType(DataType.Password)]
	[StringLength(20, MinimumLength = 6, ErrorMessage = "Enter characters between 6 to 20 in length")]
	[Required(ErrorMessage = "Please confirm your password")]
	[Compare("Password", ErrorMessage = "Password does not match")]
	[Display(Name = "Confirm Password")]
	public required string ConfirmPassword { get; set; }

	public string? AdditionalMessage { get; set; } = string.Empty;

	public string? Id { get; set; } = string.Empty;

	public string? ResetToken { get; set; } = string.Empty;

	public string? ErrorMessage { get; set; } = string.Empty;

	public string? Message { get; set; } = string.Empty;

	public bool ResetOK { get; set; }

}
