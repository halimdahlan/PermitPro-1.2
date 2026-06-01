#nullable disable

namespace PermitPro.App.Models.Ajax;

public class AjaxWorkflowStatusRequest
{
	public Guid PermitId { get; set; }
	public string Mode { get; set; }
	public string Comments { get; set; }
}
