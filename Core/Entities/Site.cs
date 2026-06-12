using Microsoft.EntityFrameworkCore;

using PermitPro.Core.Entities.Base;
using PermitPro.Core.Enums;

namespace PermitPro.Core.Entities;

public class Site : EntityBase
{
	public Guid ParentId { get; set; }

	public required string Name { get; set; }

	public string? Description { get; set; }

	public string? ContactName { get; set; }

	public string? ContactEmail { get; set; }

	public bool IsActive { get; set; }

	public SiteTypeEnum SiteType { get; set; }

	[Precision(8, 6)]
	public decimal? Latitude { get; set; }

	[Precision(9, 6)]
	public decimal? Longitude { get; set; }

	public bool ShowInMap { get; set; }

	public Company? SiteCompany { get; set; }

	public List<Permit> Permits { get; set; } = new();

	public List<UserInfo> Users { get; set; } = new();

	//public virtual List<UserSite> UsersSites { get; set; } = new();
}
