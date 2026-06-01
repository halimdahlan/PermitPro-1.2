using PermitPro.Core.Entities.Base;
using PermitPro.Core.Enums;

namespace PermitPro.Core.Entities;

public class WorkflowStep : EntityBase
{
	public required string Name { get; set; }

	public string? Description { get; set; }

	public int Duration { get; set; }

	public int StepOrder { get; set; }

	public DurationTypeEnum DurationType { get; set; }

	public bool AllowDelete {  get; set; }

	public bool AllowMove { get; set; } = true;

	public bool IsFirst { get; set; }

	public bool IsLast { get; set; }

	public Workflow? WorkflowStepWorkflow { get; set; }

	public List<UserInfo> Approvers { get; set; } = new();

}
