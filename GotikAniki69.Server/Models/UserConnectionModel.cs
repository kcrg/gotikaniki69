using System.Net.WebSockets;

namespace GotikAniki69.Server.Models;

public class UserConnectionModel(WebSocket webSocket, string nickName, string skinId, int score = 0)
{
    public WebSocket WebSocket { get; set; } = webSocket;

    public string? NickName { get; set; } = nickName;

    public string? SkinId { get; set; } = skinId;

    public int? Score { get; set; } = score;
}