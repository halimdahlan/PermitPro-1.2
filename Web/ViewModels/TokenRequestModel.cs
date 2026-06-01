using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public record TokenRequest(
    [Required] string Email,
    [Required] string Password,
    string? CompanyId
);
