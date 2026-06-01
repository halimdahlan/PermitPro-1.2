#nullable disable

namespace PermitPro.App.ViewModels;

public class PermitViewModel
{
	public Guid CompanyId { get; set; }
	public IList<string> UserRoles { get; set; }
	public EditPermitViewModel EditPermitViewModel { get; set; }
	public string GridFilter { get; set; }
	public string GridFilterField { get; set; }
}
