namespace PermitPro.Core.Data.DTO;

public record CompanyData
{
	public required Guid Id { get; init; }
	public required string Name { get; init; }
	public required string Description { get; init; }
}
