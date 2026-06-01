namespace PermitPro.App.Models.Ajax;

public class AjaxSiteModel
{
	public required string Id { get; set; }
	public required string ParentId { get; set; }
	public required string Name { get; set; }
	public required string Description { get; set; }
	public required string Contact {  get; set; }
	public required string Email { get; set; }
	public required decimal Latitude { get; set; }
	public required decimal Longitude { get; set; }
	public required bool IsActive { get; set; }
	public required int SiteType { get; set; }
	public required string CompanyId { get; set; }
	public required string Mode { get; set; }
}
