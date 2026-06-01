using Microsoft.AspNetCore.Identity;
using PermitPro.Core.Data;
using PermitPro.Core.Interfaces;

namespace PermitPro.Core.Entities;

public class UserInfo : IdentityUser, ISoftDeletable
{
	public string? FirstName { get; set; }

	public string? LastName { get; set; }

	public DateTime? DateOfBirth { get; set; } = null;

	public bool IsActive { get; set; }

	public string? Designation { get; set; }

	public string? ProfileImage { get; set; }

	public bool IsDeleted { get; set; }

	public DateTime? DeletedWhen { get; set; }

	public Guid? DeletedBy { get; set; }

	public DateTime CreatedWhen { get; set; }

	public DateTime? UpdatedWhen { get; set; }

	public Guid? CreatedBy { get; set; }

	public Guid? UpdatedBy { get; set; }

	public Company? UserCompany { get; set; }

	public List<Notification> UserNotifications { get; set; } = new();

	public List<Site> Sites { get; set; } = new();

	public List<UserRole> UserRoles { get; set; } = new();

	public List<WorkflowStep> WorkflowSteps { get; set; } = new();

	public List<AuditLog> AuditLogs { get; set; } = new();
}
