using PermitPro.Core.Interfaces;

using System.ComponentModel.DataAnnotations;

namespace PermitPro.Core.Entities.Base;

public class EntityBase : IAuditableEntity, ISoftDeletable
{
	[Key]
	public Guid Id { get; set; }

	public DateTime CreatedWhen { get; set; }

	public DateTime? UpdatedWhen { get; set; }

	public Guid? CreatedBy { get; set; }

	public Guid? UpdatedBy { get; set; }

	public bool IsDeleted { get; set; }

	public DateTime? DeletedWhen { get; set; }

	public Guid? DeletedBy { get; set; }
}
