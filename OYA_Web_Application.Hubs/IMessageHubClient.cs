using System.Threading.Tasks;

namespace OYA_Web_Application.Hubs;

public interface IMessageHubClient
{
	Task SendMessage(string user, string message);
}
