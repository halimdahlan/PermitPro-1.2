using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities;

public class Department : EntityBase
{
	public required string Name { get; set; }

	public string? Description { get; set; }

	public bool IsActive { get; set; }

	public UserInfo? DepartmentHead { get; set; }

	public UserInfo? DepartmentSupervisor { get; set; }

	public Division? DepartmentDivision { get; set; }

}
