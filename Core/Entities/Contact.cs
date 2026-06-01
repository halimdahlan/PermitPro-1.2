using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities;

public class Contact : EntityBase
{
	public string? PersonInCharge { get; set; }

	public string? Home { get; set; }

	public string? Office { get; set; }

	public string? Mobile { get; set; }

	public string? Fax { get; set; }

	public string? Email { get; set; }

}
