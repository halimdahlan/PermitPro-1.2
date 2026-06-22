using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using PermitPro.Core.Data;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

namespace PermitPro.Core.Services;

public class AppSettingsService : IAppSettingsService
{
	private readonly ApplicationDbContext _dbContext;
	private readonly IDataProtector _protector;
	private readonly IMemoryCache _cache;

	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

	public AppSettingsService(ApplicationDbContext dbContext, IDataProtectionProvider dataProtectionProvider, IMemoryCache cache)
	{
		_dbContext = dbContext;
		_protector = dataProtectionProvider.CreateProtector("AppSettings");
		_cache = cache;
	}


	// -------------------------------------------------------------------------
	// Categories
	// -------------------------------------------------------------------------

	public async Task<IEnumerable<AppSettingCategory>> GetCategoriesAsync(Guid companyId)
	{
		var cacheKey = CatsCacheKey(companyId);
		if (_cache.TryGetValue(cacheKey, out IEnumerable<AppSettingCategory>? cached))
			return cached!;

		var rows = await _dbContext.AppSettingCategories
			 .Where(c => c.CompanyId == companyId || c.CompanyId == null)
			 .OrderBy(c => c.SortOrder)
			 .ThenBy(c => c.DisplayName)
			 .ToListAsync();

		// Company-specific categories override globals with the same Name
		var result = rows
			 .GroupBy(c => c.Name)
			 .Select(g => g.OrderByDescending(c => c.CompanyId.HasValue).First())
			 .OrderBy(c => c.SortOrder)
			 .ThenBy(c => c.DisplayName)
			 .ToList();

		_cache.Set(cacheKey, (IEnumerable<AppSettingCategory>)result, CacheDuration);
		return result;
	}

	public async Task UpsertCategoryAsync(AppSettingCategory category)
	{
		var existing = await _dbContext.AppSettingCategories
			 .FirstOrDefaultAsync(c => c.Id == category.Id);

		if (existing is null)
		{
			if (category.Id == Guid.Empty)
				category.Id = Guid.NewGuid();
			_dbContext.AppSettingCategories.Add(category);
		}
		else
		{
			existing.Name = category.Name;
			existing.DisplayName = category.DisplayName;
			existing.Description = category.Description;
			existing.Icon = category.Icon;
			existing.SortOrder = category.SortOrder;
			existing.CompanyId = category.CompanyId;
		}

		await _dbContext.SaveChangesAsync();
		InvalidateCategoryCache(category.CompanyId);
	}

	public async Task DeleteCategoryAsync(Guid id)
	{
		var category = await _dbContext.AppSettingCategories.FindAsync(id);
		if (category is null) return;

		_dbContext.AppSettingCategories.Remove(category);
		await _dbContext.SaveChangesAsync();
		InvalidateCategoryCache(category.CompanyId);
	}


	// -------------------------------------------------------------------------
	// Settings
	// -------------------------------------------------------------------------

	public async Task<string?> GetValueAsync(Guid companyId, string categoryName, string key)
	{
		var settings = await GetAllSettingsFlatAsync(companyId);

		var match = settings.FirstOrDefault(s =>
			 string.Equals(s.Category?.Name, categoryName, StringComparison.OrdinalIgnoreCase) &&
			 string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase) &&
			 s.CompanyId == companyId)
			 ?? settings.FirstOrDefault(s =>
			 string.Equals(s.Category?.Name, categoryName, StringComparison.OrdinalIgnoreCase) &&
			 string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase) &&
			 s.CompanyId == null);

		if (match is null) return null;
		return Decrypt(match);
	}

	public async Task<int> GetIntAsync(Guid companyId, string categoryName, string key)
	{
		var settings = await GetAllSettingsFlatAsync(companyId);

		var match = settings.FirstOrDefault(s =>
			 string.Equals(s.Category?.Name, categoryName, StringComparison.OrdinalIgnoreCase) &&
			 string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase) &&
			 s.CompanyId == companyId)
			 ?? settings.FirstOrDefault(s =>
			 string.Equals(s.Category?.Name, categoryName, StringComparison.OrdinalIgnoreCase) &&
			 string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase) &&
			 s.CompanyId == null);

		if (match is null) return 0;
		var decrypt = Decrypt(match);

		return int.TryParse(decrypt, out var result) ? result : 0;
	}

	public async Task<IEnumerable<AppSetting>> GetSettingsAsync(Guid companyId, Guid categoryId)
	{
		var all = await GetAllSettingsFlatAsync(companyId);

		var companyRows = all.Where(s => s.CategoryId == categoryId && s.CompanyId == companyId).ToList();
		var globalRows = all.Where(s => s.CategoryId == categoryId && s.CompanyId == null).ToList();

		// Merge: company-specific row wins over global for the same Key
		var merged = globalRows
			 .Where(g => !companyRows.Any(c => string.Equals(c.Key, g.Key, StringComparison.OrdinalIgnoreCase)))
			 .Concat(companyRows)
			 .OrderBy(s => s.SortOrder)
			 .ThenBy(s => s.DisplayName)
			 .Select(s => DecryptForDisplay(s))
			 .ToList();

		return merged;
	}

	public async Task UpsertSettingAsync(AppSetting setting)
	{
		var existing = await _dbContext.AppSettings
			 .FirstOrDefaultAsync(s => s.Id == setting.Id);

		if (existing is null)
		{
			if (setting.Id == Guid.Empty)
				setting.Id = Guid.NewGuid();
			if (setting.IsEncrypted && !string.IsNullOrEmpty(setting.Value))
				setting.Value = _protector.Protect(setting.Value);
			_dbContext.AppSettings.Add(setting);
		}
		else
		{
			existing.Key = setting.Key;
			existing.DisplayName = setting.DisplayName;
			existing.DataType = setting.DataType;
			existing.IsEncrypted = setting.IsEncrypted;
			existing.SortOrder = setting.SortOrder;
			existing.CompanyId = setting.CompanyId;
			existing.CategoryId = setting.CategoryId;

			if (setting.IsEncrypted && !string.IsNullOrEmpty(setting.Value))
				existing.Value = _protector.Protect(setting.Value);
			else
				existing.Value = setting.Value;
		}

		await _dbContext.SaveChangesAsync();
		InvalidateSettingsCache(setting.CompanyId, setting.CategoryId);
	}

	public async Task DeleteSettingAsync(Guid id)
	{
		var setting = await _dbContext.AppSettings.FindAsync(id);
		if (setting is null) return;

		_dbContext.AppSettings.Remove(setting);
		await _dbContext.SaveChangesAsync();
		InvalidateSettingsCache(setting.CompanyId, setting.CategoryId);
	}


	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private async Task<List<AppSetting>> GetAllSettingsFlatAsync(Guid companyId)
	{
		var cacheKey = ValsCacheKey(companyId);
		if (_cache.TryGetValue(cacheKey, out List<AppSetting>? cached))
			return cached!;

		var rows = await _dbContext.AppSettings
			 .Include(s => s.Category)
			 .Where(s => s.CompanyId == companyId || s.CompanyId == null)
			 .ToListAsync();

		_cache.Set(cacheKey, rows, CacheDuration);
		return rows;
	}

	private string? Decrypt(AppSetting setting)
	{
		if (!setting.IsEncrypted || string.IsNullOrEmpty(setting.Value))
			return setting.Value;
		try { return _protector.Unprotect(setting.Value); }
		catch { return null; }
	}

	private AppSetting DecryptForDisplay(AppSetting setting)
	{
		if (!setting.IsEncrypted || string.IsNullOrEmpty(setting.Value))
			return setting;

		// Return a shallow copy with the decrypted value so the cached entity is not mutated
		return new AppSetting
		{
			Id = setting.Id,
			CategoryId = setting.CategoryId,
			Category = setting.Category,
			CompanyId = setting.CompanyId,
			Key = setting.Key,
			DisplayName = setting.DisplayName,
			DataType = setting.DataType,
			IsEncrypted = setting.IsEncrypted,
			SortOrder = setting.SortOrder,
			Value = Decrypt(setting)
		};
	}

	private void InvalidateCategoryCache(Guid? companyId)
	{
		_cache.Remove(CatsCacheKey(companyId ?? Guid.Empty));
		_cache.Remove(CatsCacheKey(Guid.Empty));
	}

	private void InvalidateSettingsCache(Guid? companyId, Guid categoryId)
	{
		_cache.Remove(ValsCacheKey(companyId ?? Guid.Empty));
		_cache.Remove(ValsCacheKey(Guid.Empty));
	}

	private static string CatsCacheKey(Guid companyId) => $"appsettings:cats:{companyId}";
	private static string ValsCacheKey(Guid companyId) => $"appsettings:vals:{companyId}";

}
