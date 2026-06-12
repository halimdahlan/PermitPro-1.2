#nullable disable

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using PermitPro.Core.Data;
using PermitPro.Core.Interfaces;

using System.Security.Claims;

namespace PermitPro.Core.Interceptors;

public sealed class UpdateAuditableEntitiesInterceptor : SaveChangesInterceptor
{
	private readonly IHttpContextAccessor _contextAccessor;

	public UpdateAuditableEntitiesInterceptor(IHttpContextAccessor contextAccessor)
	{
		_contextAccessor = contextAccessor;
	}


	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
	{
		if (eventData.Context is null)
			return base.SavingChanges(eventData, result);

		ConvertDeletedToSoftDelete(eventData.Context);
		ApplyAuditFields(eventData.Context);

		return base.SavingChanges(eventData, result);
	}


	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		if (eventData.Context is null)
			return base.SavingChangesAsync(eventData, result, cancellationToken);

		ConvertDeletedToSoftDelete(eventData.Context);
		ApplyAuditFields(eventData.Context);

		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}


	// Intercepts Remove() / RemoveRange() for ISoftDeletable entities and converts
	// the physical DELETE into a soft-delete UPDATE (if UseSoftDelete flag is TRUE).
	// If UseSoftDelete is FALSE, performs a hard delete (actual removal from database).
	private void ConvertDeletedToSoftDelete(DbContext dbContext)
	{
		var userId = GetCurrentUserId();
		var now = DateTime.UtcNow;

		// Check if we should use soft delete (default is true)
		if (dbContext is ApplicationDbContext appContext && !appContext.UseSoftDelete)
		{
			// Hard delete mode - don't convert to soft delete, allow physical removal
			return;
		}

		foreach (var entry in dbContext.ChangeTracker.Entries<ISoftDeletable>()
			.Where(e => e.State == EntityState.Deleted))
		{
			entry.State = EntityState.Modified;
			entry.Entity.IsDeleted = true;
			entry.Entity.DeletedWhen = now;
			entry.Entity.DeletedBy = userId;
		}
	}


	private void ApplyAuditFields(DbContext dbContext)
	{
		var userId = GetCurrentUserId();
		var now = DateTime.UtcNow.ToUniversalTime();

		foreach (var entry in dbContext.ChangeTracker.Entries<IAuditableEntity>())
		{
			if (entry.State == EntityState.Added)
			{
				if (entry.Property(e => e.Id).CurrentValue == Guid.Empty)
					entry.Property(e => e.Id).CurrentValue = Guid.NewGuid();

				entry.Property(e => e.CreatedWhen).CurrentValue = now;

				if (userId.HasValue)
					entry.Property(e => e.CreatedBy).CurrentValue = userId.Value;
			}

			if (entry.State == EntityState.Modified)
			{
				entry.Property(e => e.UpdatedWhen).CurrentValue = now;

				if (userId.HasValue)
					entry.Property(e => e.UpdatedBy).CurrentValue = userId.Value;
			}
		}
	}


	private Guid? GetCurrentUserId()
	{
		var idStr = _contextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
		return Guid.TryParse(idStr, out var id) ? id : null;
	}
}
