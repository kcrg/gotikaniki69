using System.Net.WebSockets;

namespace GotikAniki69.Server.Models;

public class UserConnectionModel(WebSocket webSocket, string nickName, string skinId)
{
    public WebSocket WebSocket { get; set; } = webSocket;

    public string? NickName { get; set; } = nickName;

    public string? SkinId { get; set; } = skinId;
}