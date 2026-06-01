using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities;

public class Division : EntityBase
{
	public required string Name { get; set; }

	public string? Description { get; set; }

	public bool IsActive { get; set; }

	public UserInfo? DivisionHead { get; set; }

	public UserInfo? DivisionSupervisor { get; set; }

	public Site? DivisionSite { get; set; }

	public List<Department>? DivisionDepartments { get; set; }

}
