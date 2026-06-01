#nullable disable

using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities;

public class Workflow : EntityBase
{
	public required string Name { get; set; }

	public string Description { get; set; }

	public bool IsActive { get; set; }

	public bool HasCertificate { get; set; }

	public Company WorkflowCompany { get; set; }

	public List<WorkflowStep> WorkflowSteps { get; set; } = new();

	public List<Permit> WorkflowPermits { get; set; } = new();

	public List<WorkflowHistory> WorkflowHistories { get; set; } = new();

}
