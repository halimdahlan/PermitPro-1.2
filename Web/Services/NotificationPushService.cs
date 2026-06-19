#nullable disable

using Microsoft.AspNetCore.SignalR;

using PermitPro.App.Hubs;
using PermitPro.Core.Interfaces;

namespace PermitPro.App.Services;

public class NotificationPushService : INotificationPushService
{
	private readonly IHubContext<NotificationHub> _hub;

	public NotificationPushService(IHubContext<NotificationHub> hub)
		=> _hub = hub;

	public async Task PushAsync(string userId, string title, string message)
	{
		if (string.IsNullOrEmpty(userId)) return;

		await _hub.Clients.User(userId).SendAsync("ReceiveNotification", new
		{
			title,
			message
		});
	}
}
