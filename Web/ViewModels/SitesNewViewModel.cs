using PermitPro.Core.Entities;

using System.ComponentModel.DataAnnotations;

namespace PermitPro.App.ViewModels;

public class SitesNewViewModel() : BaseViewModel
{
	public Guid? ParentId { get; set; }

	[Required(ErrorMessage = "Name is required.")]
	public string Name { get; set; } = string.Empty;

	public string? Description { get; set; }
	[Required(ErrorMessage = "Contact name is required.")]
	public string ContactName { get; set; } = string.Empty;

	[Required(ErrorMessage = "Contact email is required.")]
	[EmailAddress(ErrorMessage = "Invalid email address.")]
	public string ContactEmail { get; set; } = string.Empty;

	public decimal? Latitude { get; set; }

	public decimal? Longitude { get; set; }

	public bool ShowInMap { get; set; } = true;

	public bool IsActive { get; set; } = true;

	public static SitesNewViewModel FromSite(Site s) => new()
	{
		Name = s.Name,
		Description = s.Description,
		ContactName = s.ContactName ?? string.Empty,
		ContactEmail = s.ContactEmail ?? string.Empty,
		Latitude = s.Latitude,
		Longitude = s.Longitude,
		ShowInMap = s.ShowInMap,
		IsActive = s.IsActive
	};
}