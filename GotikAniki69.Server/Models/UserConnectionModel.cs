using System.Net.WebSockets;

namespace GotikAniki69.Server.Models;

public class UserConnectionModel(WebSocket webSocket, string userName)
{
    public WebSocket WebSocket { get; set; } = webSocket;

    public string? UserName { get; set; } = userName;
}
