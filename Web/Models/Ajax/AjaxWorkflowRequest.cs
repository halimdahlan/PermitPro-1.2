#nullable disable

namespace PermitPro.App.Models.Ajax;

public class AjaxWorkflowRequest
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public bool IsActive { get; set; }
	public bool HasCertificate { get; set; }
}
