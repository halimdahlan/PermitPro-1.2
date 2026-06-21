#nullable disable

using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class RolesGridViewModel
{
	public string Id { get; set; }

	[Display(Name = "NAME")]
	public string Name { get; set; }

	[Display(Name = "DESCRIPTION")]
	public string Description { get; set; }

	[Display(Name = "USERS")]
	public int NumOfUsers { get; set; }

	[Display(Name = "SYSTEM ROLE?")]
	public bool IsSystemRole { get; set; }

	[Display(Name = "DATE CREATED")]
	public DateTime CreatedWhen { get; set; }

	[Display(Name = "ACTION")]
	public string ActionIcons { get; set; }
}


public class UsersGridViewModel
{
	public string Id { get; set; }

	[Display(Name = "NAME")]
	public string Name { get; set; }

	[Display(Name = "EMAIL")]
	public string Email { get; set; }

	[Display(Name = "LOCATION")]
	public string Location { get; set; }

	[Display(Name = "DESIGNATION")]
	public string Designation { get; set; }

	[Display(Name = "ROLE")]
	public string Roles { get; set; }

	[Display(Name = "SITES")]
	public string Sites { get; set; }

	[Display(Name = "IS SECURED?")]
	public bool IsSecured { get; set; }

	[Display(Name = "IS ACTIVE?")]
	public bool IsActive { get; set; }

	[Display(Name = "DATE CREATED")]
	public DateTime CreatedWhen { get; set; }

	[Display(Name = "ACTION")]
	public string ActionIcons { get; set; }

	public bool HasPermits { get; set; }
}
