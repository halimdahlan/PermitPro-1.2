#nullable disable

using PermitPro.Core.Enums;

namespace PermitPro.App.ViewModels;

public class WorkflowStepEditViewModel
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public bool AllowDelete { get; set; }
	public bool AllowMove { get; set; }
	public int Duration { get; set; }
	public int StepOrder { get; set; }
	public DurationTypeEnum DurationType { get; set; }
	public DateTime? CreatedWhen { get; set; }
	public DateTime? UpdatedWhen { get; set; }
	public Guid? CreatedBy { get; set; }
	public Guid? UpdatedBy { get; set; }

}
