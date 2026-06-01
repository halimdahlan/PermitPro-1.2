using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities;

public class Address : EntityBase
{
	public required string Name { get; set; }

	public string? Line1 { get; set; }

	public string? Line2 { get; set; }

	public string? Line3 { get; set; }

	public string? PostalCode { get; set; }

	public string? City { get; set; }

	public string? State { get; set; }

	public string? Country { get; set; }

	public Contact? AddressContact { get; set; }

	public Company? AddressCompany { get; set; }

}
