namespace PermitPro.Core.Entities;

public class SitePermit
{
	public Guid SiteId { get; set; }
	public Site? Site { get; set; }

	public Guid PermitId { get; set; }
	public Permit? Permit { get; set; }
}
