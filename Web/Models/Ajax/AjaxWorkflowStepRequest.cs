#nullable disable

namespace PermitPro.App.Models.Ajax;

public class AjaxWorkflowStepRequest
{
	public Guid? Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public int DurationType { get; set; }
	public int Duration {  get; set; }
	public Guid? WorkflowId { get; set; }
}
