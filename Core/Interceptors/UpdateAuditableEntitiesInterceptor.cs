#nullable disable

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
		DbContext dbContext = eventData.Context;

		if (dbContext is null)
		{
			return base.SavingChanges(eventData, result);
		}

		var entries = dbContext.ChangeTracker.Entries<IAuditableEntity>();

		foreach (var entry in entries)
		{
			if (entry.State == EntityState.Added)
			{
				if (entry.Property(e => e.Id).CurrentValue == Guid.Empty)
				{
					entry.Property(e => e.Id).CurrentValue = Guid.NewGuid();
				}

				entry.Property(e => e.CreatedWhen).CurrentValue = DateTime.UtcNow.ToUniversalTime();
				entry.Property(e => e.CreatedBy).CurrentValue = Guid.Parse(_contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
			}

			if (entry.State == EntityState.Modified)
			{
				entry.Property(e => e.UpdatedWhen).CurrentValue = DateTime.UtcNow.ToUniversalTime();
				entry.Property(e => e.UpdatedBy).CurrentValue = Guid.Parse(_contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
			}
		}

		return base.SavingChanges(eventData, result);
	}


	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
	{
		DbContext dbContext = eventData.Context;

		if (dbContext is null)
		{
			return base.SavingChangesAsync(eventData, result, cancellationToken);
		}

		var entries = dbContext.ChangeTracker.Entries<IAuditableEntity>();

		foreach (var entry in entries)
		{
			if (entry.State == EntityState.Added)
			{
				if (entry.Property(e => e.Id).CurrentValue == Guid.Empty)
				{
					entry.Property(e => e.Id).CurrentValue = Guid.NewGuid();
				}

				entry.Property(e => e.CreatedWhen).CurrentValue = DateTime.UtcNow.ToUniversalTime();
				entry.Property(e => e.CreatedBy).CurrentValue = Guid.Parse(_contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
			}

			if (entry.State == EntityState.Modified)
			{
				entry.Property(e => e.UpdatedWhen).CurrentValue = DateTime.UtcNow.ToUniversalTime();
				entry.Property(e => e.UpdatedBy).CurrentValue = Guid.Parse(_contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
			}
		}

		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}
}
