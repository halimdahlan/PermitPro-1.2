using PermitPro.Core.Entities;

namespace PermitPro.App.ViewModels;

public class SitesViewModel : BaseViewModel
{
	public string? ParentId { get; set; }
	public Site? SiteParent { get; set; }
	public string? Id { get; set; }
	public string? Name { get; set; }
	public string? Description { get; set; }
	public string? ContactName { get; set; }
	public string? ContactEmail { get; set; }
	public bool IsActive { get; set; }
}
