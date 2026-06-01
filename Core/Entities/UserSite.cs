using Microsoft.EntityFrameworkCore;

namespace PermitPro.Core.Entities
{
	[Keyless]
	public class UserSite
	{
		public string? UserId { get; set; }
		public virtual UserInfo? User { get; set; }

		public Guid SiteId { get; set; }
		public virtual Site? Site { get; set; }
	}
}
