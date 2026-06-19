#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PermitPro.App.Hubs;

[Authorize]
public class NotificationHub : Hub
{
	public override async Task OnConnectedAsync()
	{
		if (Context.UserIdentifier != null)
			await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier);

		await base.OnConnectedAsync();
	}
}
