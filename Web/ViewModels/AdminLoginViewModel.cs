using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "This field is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [DataType(DataType.EmailAddress)]
    [Display(Name = "Email address")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "This field is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public required string Password { get; set; }

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
