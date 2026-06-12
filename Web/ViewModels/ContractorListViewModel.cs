#nullable disable

using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class ContractorListViewModel
{
	[Display(Name = "ID")]
	public string Id { get; set; }

	[Display(Name = "NAME")]
	public string Name { get; set; }

	[Display(Name = "EMAIL")]
	public string Email { get; set; }

	[Display(Name = "LOCATION")]
	public string Location { get; set; }

	[Display(Name = "IS SECURED?")]
	public string IsSecured { get; set; }

	[Display(Name = "IS ACTIVE?")]
	public string IsActiveIcons { get; set; }

	[Display(Name = "DATE CREATED")]
	public DateTime CreatedWhen { get; set; }

	[Display(Name = "ACTION")]
	public string ActionIcons { get; set; }

	public bool IsActive { get; set; }
}
