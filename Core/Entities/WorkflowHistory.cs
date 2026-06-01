using PermitPro.Core.Entities.Base;
using PermitPro.Core.Enums;

namespace PermitPro.Core.Entities;

public class WorkflowHistory : EntityBase
{
	public Permit? Permit { get; set; }
	public string? Comments { get; set; }

	public Workflow? HistoryWorkflow { get; set; }

	public WorkflowStep? HistoryWorkflowStep { get; set; }

	public WorkflowStatusEnum Status { get; set; }

	public DateTime? ApprovedWhen { get; set; }

	public DateTime? RejectedWhen { get; set; }

}
