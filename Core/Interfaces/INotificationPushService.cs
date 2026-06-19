#nullable disable

namespace PermitPro.Core.Interfaces;

public interface INotificationPushService
{
	Task PushAsync(string userId, string title, string message);
}
