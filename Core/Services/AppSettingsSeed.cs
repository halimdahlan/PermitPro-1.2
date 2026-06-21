using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PermitPro.Core.Data;
using PermitPro.Core.Entities;

namespace PermitPro.Core.Services;

public static class AppSettingsSeed
{
    public static async Task SeedDefaultsAsync(IServiceProvider sp, IConfiguration config)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("AppSettings");

        if (await db.AppSettingCategories.AnyAsync())
            return;

        var emailCategoryId = Guid.NewGuid();
        var generalCategoryId = Guid.NewGuid();

        var categories = new List<AppSettingCategory>
        {
            new()
            {
                Id = emailCategoryId,
                CompanyId = null,
                Name = "email",
                DisplayName = "Email",
                Description = "SMTP email server configuration",
                Icon = "fa-envelope",
                SortOrder = 1
            },
            new()
            {
                Id = generalCategoryId,
                CompanyId = null,
                Name = "general",
                DisplayName = "General",
                Description = "General application settings",
                Icon = "fa-sliders",
                SortOrder = 2
            }
        };

        await db.AppSettingCategories.AddRangeAsync(categories);

        var emailSection = config.GetSection("EmailSettings");
        var emailSettings = new List<AppSetting>
        {
            new() { Id = Guid.NewGuid(), CategoryId = emailCategoryId, CompanyId = null, Key = "server",      DisplayName = "SMTP Server",   DataType = "text",     IsEncrypted = false, SortOrder = 1, Value = emailSection["Server"] },
            new() { Id = Guid.NewGuid(), CategoryId = emailCategoryId, CompanyId = null, Key = "port",        DisplayName = "SMTP Port",     DataType = "number",   IsEncrypted = false, SortOrder = 2, Value = emailSection["Port"] },
            new() { Id = Guid.NewGuid(), CategoryId = emailCategoryId, CompanyId = null, Key = "senderName",  DisplayName = "Sender Name",   DataType = "text",     IsEncrypted = false, SortOrder = 3, Value = emailSection["SenderName"] },
            new() { Id = Guid.NewGuid(), CategoryId = emailCategoryId, CompanyId = null, Key = "senderEmail", DisplayName = "Sender Email",  DataType = "email",    IsEncrypted = false, SortOrder = 4, Value = emailSection["SenderEmail"] },
            new() { Id = Guid.NewGuid(), CategoryId = emailCategoryId, CompanyId = null, Key = "userName",    DisplayName = "Username",      DataType = "text",     IsEncrypted = false, SortOrder = 5, Value = emailSection["UserName"] },
            new() { Id = Guid.NewGuid(), CategoryId = emailCategoryId, CompanyId = null, Key = "password",    DisplayName = "Password",      DataType = "password", IsEncrypted = true,  SortOrder = 6, Value = Encrypt(protector, emailSection["Password"]) },
        };

        await db.AppSettings.AddRangeAsync(emailSettings);
        await db.SaveChangesAsync();
    }

    private static string? Encrypt(IDataProtector protector, string? value)
        => string.IsNullOrEmpty(value) ? value : protector.Protect(value);
}
