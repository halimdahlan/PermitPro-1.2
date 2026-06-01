using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities;

public class Certificate : EntityBase
{
	public required string Name { get; set; }

	public string? Description { get; set; }

	public string? SerializedData { get; set; }

	public List<Permit> Permits { get; set; } = new();

}
