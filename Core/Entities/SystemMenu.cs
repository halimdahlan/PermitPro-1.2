using PermitPro.Core.Data;
using PermitPro.Core.Entities.Base;

using System.ComponentModel.DataAnnotations.Schema;

namespace PermitPro.Core.Entities;

public class SystemMenu : EntityBase
{

	public Guid? ParentId { get; set; }

	public required string Name { get; set; }

	public string? Description { get; set; }

	public string? IconName { get; set;}

	public string? ControllerName {  get; set; }

	public string? ActionName { get; set; }

	public int MenuOrder { get; set; }

	public List<Role>? Roles { get; set; } = new();

	[NotMapped]
	public bool HasChildren { get; set; }

	[NotMapped]
	public List<SystemMenu>? Children { get; set; }

}
