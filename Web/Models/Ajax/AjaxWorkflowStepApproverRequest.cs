#nullable disable

namespace PermitPro.App.Models.Ajax;

public class AjaxWorkflowStepApproverRequest
{
	public Guid WorkflowStepId { get; set; }
	public string[] Approvers { get; set; }
}
