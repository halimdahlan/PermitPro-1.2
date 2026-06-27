#nullable disable

using PermitPro.Core.Entities;

namespace PermitPro.App.ViewModels;

public class AdminCompanySettingsViewModel
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; }
    public string CompanyLogoFileName { get; set; }
    public IEnumerable<AppSettingCategory> Categories { get; set; } = [];
}
