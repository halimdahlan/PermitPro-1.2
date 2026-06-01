#nullable disable

namespace PermitPro.App.ViewModels;

public class WorkflowEditViewModel
{
	public string WorkflowId { get; set; }
	public string WorkflowName { get; set; }
	public string WorkflowDescription { get; set; }
	public bool WorkflowIsActive { get; set; }
	public bool WorkflowHasCertificate { get; set; }
	public List<WorkflowStepEditViewModel> WorkflowSteps { get; set; }
}
