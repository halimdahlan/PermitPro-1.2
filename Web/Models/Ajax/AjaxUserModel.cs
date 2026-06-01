namespace PermitPro.App.Models.Ajax;

public class AjaxUserModel
{
	public string? Id { get; set; }
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string Email { get; set; }
	public string? Password { get; set; }
	public string? Designation { get; set; }
	public bool IsActive { get; set; }
	public required string Role { get; set; }
	public required string[] Locations { get; set; }
}
