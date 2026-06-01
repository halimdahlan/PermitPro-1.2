namespace PermitPro.Core.Data.DTO;

public record UserData
{
	public required string Id { get; init; }
	public required string Name { get; init; }
	public required string Email { get; init; }
	public required string FirstName { get; init; }
	public required string LastName { get; init; }
	public required string FullName { get; init; }
	public required string Roles { get; init; }
	public bool IsActive { get; init; }
	public required CompanyData Company { get; init; }
}
