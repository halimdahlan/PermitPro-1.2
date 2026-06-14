using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class ManageUserViewNodel
{
  [Required(ErrorMessage = "First name is required")]
  public required string FirstName { get; set; }

  [Required(ErrorMessage = "Last name is required")]
  public required string LastName { get; set; }

  [Required(ErrorMessage = "Email is required")]
  [EmailAddress]
  public required string Email { get; set; }

  public string? Designation { get; set; } = string.Empty;

  [Required(ErrorMessage = "Role is required")]
  public required string UserRole { get; set; }

  public string? UserPassword { get; set; }

  public bool IsEdit { get; set; } = false;

  public Guid CompanyID { get; set; }

  public List<RoleDropDown> Roles { get; set; } = new();

  public string? Locations { get; set; } = string.Empty;
}


public class RoleDropDown
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}