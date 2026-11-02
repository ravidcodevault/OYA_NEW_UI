using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace OYA_Web_Application.Hubs;

public class MessageHub : Hub
{
	public async Task SendMessage(string user, string message)
	{
		await base.Clients.All.SendAsync("ReceiveMessage", user, message);
	}
}
