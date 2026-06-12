namespace PermitPro.Core.Interfaces;

public interface IAuditableEntity
{
	Guid Id { get; set; }

	DateTime CreatedWhen { get; set; }
	DateTime? UpdatedWhen { get; set; }

	Guid? CreatedBy { get; set; }
	Guid? UpdatedBy { get; set; }
}
