#nullable disable

using System.ComponentModel.DataAnnotations;

namespace PermitPro.Core.Entities;

public class AppSettingCategory
{
    [Key]
    public Guid Id { get; set; }

    public Guid? CompanyId { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string DisplayName { get; set; }

    public string Description { get; set; }

    public string Icon { get; set; }

    public int SortOrder { get; set; }

    public ICollection<AppSetting> Settings { get; set; } = new List<AppSetting>();
}
