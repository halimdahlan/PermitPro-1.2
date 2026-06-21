namespace PermitPro.App.Models.Ajax;

public class AjaxAppSettingCategoryModel
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}
