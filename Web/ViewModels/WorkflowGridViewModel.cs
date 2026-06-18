#nullable disable

using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class WorkflowGridViewModel
{
	public Guid Id { get; set; }

	[Display(Name = "WORKFLOW NAME")]
	public string Name { get; set; }

	[Display(Name = "DESCRIPTION")]
	public string Description { get; set; }

	[Display(Name = "IS ACTIVE?")]
	public bool IsActive { get; set; }

	[Display(Name = "PTW WITH CERTIFICATE?")]
	public bool HasCertificates { get; set; }

	[Display(Name = "DATE CREATED")]
	public DateTime CreatedWhen { get; set; }

	[Display(Name = "ACTION")]
	public string ActionIcons { get; set; }

	public bool HasPermits { get; set; }
}
