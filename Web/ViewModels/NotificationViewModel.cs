#nullable disable

namespace PermitPro.App.ViewModels;

public class NotificationViewModel
{
	public Guid Id { get; set; }
	public string Title { get; set; }
	public string Message { get; set; }
	public string Url { get; set; }
	public bool IsRead { get; set; }
	public string CreatedWhen { get; set; }
}

public class NotificationsPageViewModel
{
	public List<NotificationViewModel> Notifications { get; set; } = [];
	public int UnreadCount { get; set; }
}
