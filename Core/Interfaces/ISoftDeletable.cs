namespace PermitPro.Core.Interfaces;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedWhen { get; set; }
    Guid? DeletedBy { get; set; }
}
