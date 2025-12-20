using System.Net.WebSockets;

namespace GotikAniki69.Server.Game;

public sealed class UserConnection(WebSocket ws, string nick, string skinId)
{
    public WebSocket Ws { get; } = ws;

    public string Nick { get; } = nick;

    public string SkinId { get; } = skinId;

    public int Score { get; set; }

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (Ws.State != WebSocketState.Open)
            return;

        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (Ws.State == WebSocketState.Open)
                await Ws.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
