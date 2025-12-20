using System.Buffers;
using System.Net.WebSockets;
using GotikAniki69.Server.Game;
using GotikAniki69.Server.Models;
using GotikAniki69.Server.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace GotikAniki69.Server.Web;

public static class WebSocketGameEndpoint
{
    public static IEndpointConventionBuilder MapGameWebSocket(this IEndpointRouteBuilder app, string pattern)
        => app.Map(pattern, HandleAsync);

    private static async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var name = context.Request.Query["name"].ToString();
        var skinId = context.Request.Query["skinId"].ToString();

        if (string.IsNullOrWhiteSpace(name) || name.Length is < 2 or > 24)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (string.IsNullOrWhiteSpace(skinId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var game = context.RequestServices.GetRequiredService<GameState>();

        using var ws = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        var id = Guid.NewGuid();

        var user = game.AddUser(id, ws, name.Trim(), skinId.Trim());

        try
        {
            // Hello -> caller
            await user.SendAsync(JsonBytes.Serialize(
                new ResponseModel { Type = nameof(MessageTypeEnum.Hello) },
                JsonContext.Default.ResponseModel
            ), context.RequestAborted).ConfigureAwait(false);

            // initial BallMovement -> caller
            var (bx, by) = game.GetBall();
            await user.SendAsync(JsonBytes.Serialize(
                new ResponseModel
                {
                    Type = nameof(MessageTypeEnum.BallMovement),
                    Payload = new PayloadModel { X = bx, Y = by }
                },
                JsonContext.Default.ResponseModel
            ), context.RequestAborted).ConfigureAwait(false);

            // receive loop (pooled buffer, bez stringa)
            var buffer = ArrayPool<byte>.Shared.Rent(2048);
            try
            {
                while (ws.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(buffer, context.RequestAborted).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.MessageType != WebSocketMessageType.Text || result.EndOfMessage == false)
                        continue;

                    CoordinatesModel? msg;
                    try
                    {
                        msg = JsonSerializer.Deserialize(
                            buffer.AsSpan(0, result.Count),
                            JsonContext.Default.CoordinatesModel);
                    }
                    catch
                    {
                        continue;
                    }

                    if (msg is null)
                        continue;

                    var (ballMove, hit) = game.ProcessHit(id, msg);

                    if (ballMove is not null)
                        await game.BroadcastAsync(ballMove, context.RequestAborted).ConfigureAwait(false);

                    await game.BroadcastAsync(hit, context.RequestAborted).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        finally
        {
            game.RemoveUser(id);

            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }
    }
}
