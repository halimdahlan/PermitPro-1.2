using PermitPro.Core.Entities.Base;

namespace PermitPro.Core.Entities;

public class Notification : EntityBase
{
	public required string Title { get; set; }

	public required string Message { get; set; }

	public string? Url { get; set; }

	public bool IsRead { get; set; }

	public bool IsArchived { get; set; }

	public UserInfo? NotificationUser { get; set; }

}
