using System.Collections.Concurrent;
using System.Net.WebSockets;
using GotikAniki69.Server.Models;
using GotikAniki69.Server.Serialization;

namespace GotikAniki69.Server.Game;

public sealed class GameState
{
    private readonly ConcurrentDictionary<Guid, UserConnection> _connections = new();
    private readonly Random _rng = new();

    private readonly object _ballLock = new();
    private double _ballX = 1000;
    private double _ballY = 400;

    public (double X, double Y) GetBall()
    {
        lock (_ballLock) return (_ballX, _ballY);
    }

    public UserConnection AddUser(Guid id, WebSocket ws, string nick, string skinId)
    {
        var user = new UserConnection(ws, nick, skinId);
        _connections[id] = user;
        return user;
    }

    public void RemoveUser(Guid id) => _connections.TryRemove(id, out _);

    public async Task BroadcastAsync(ResponseModel msg, CancellationToken ct)
    {
        // 1 serializacja na broadcast
        var bytes = JsonBytes.Serialize(msg, JsonContext.Default.ResponseModel);

        foreach (var c in _connections.Values)
        {
            if (c.Ws.State == WebSocketState.Open)
                await c.SendAsync(bytes, ct).ConfigureAwait(false);
        }
    }

    public (ResponseModel? BallMove, ResponseModel Hit) ProcessHit(Guid senderId, CoordinatesModel coords)
    {
        if (!_connections.TryGetValue(senderId, out var sender))
        {
            return (null, new ResponseModel
            {
                Type = nameof(MessageTypeEnum.Hit),
                Payload = new PayloadModel
                {
                    Index = 2,
                    NickName = "???",
                    SkinId = "1",
                    X = Math.Max(0, coords.X),
                    Y = Math.Max(0, coords.Y),
                }
            });
        }

        var payload = new PayloadModel
        {
            Index = _rng.Next(1, 7),
            NickName = $"{sender.Nick} ({sender.Score})",
            SkinId = sender.SkinId,
            X = Math.Max(0, coords.X),
            Y = Math.Max(0, coords.Y)
        };

        ResponseModel? ballMove = null;

        lock (_ballLock)
        {
            var dx = _ballX - payload.X;
            var dy = _ballY - payload.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < 100 && dist > 0.0001)
            {
                var force = _rng.Next(20, 60);
                var newX = _ballX + (dx / dist * force);
                var newY = _ballY + (dy / dist * force);

                // out of pitch
                if (newX < 0 || newY < 0 || newY > 800 || newX > 1600)
                {
                    newX = _rng.Next(600, 1200);
                    newY = _rng.Next(300, 750);
                }

                // goal score
                if (newX > 100 && newX < 150 && newY > 300 && newY < 500)
                {
                    newX = _rng.Next(600, 1200);
                    newY = _rng.Next(300, 750);

                    payload.Index = 8;
                    sender.Score++;
                    payload.NickName = $"{sender.Nick} ({sender.Score})";
                }

                _ballX = newX;
                _ballY = newY;

                ballMove = new ResponseModel
                {
                    Type = nameof(MessageTypeEnum.BallMovement),
                    Payload = new PayloadModel { X = _ballX, Y = _ballY }
                };
            }
        }

        var hit = new ResponseModel
        {
            Type = nameof(MessageTypeEnum.Hit),
            Payload = payload
        };

        return (ballMove, hit);
    }
}
