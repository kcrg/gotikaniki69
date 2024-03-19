using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GotikAniki69.Server.Models;

namespace GotikAniki69.Server;

public static class Program
{
    private static readonly ConcurrentDictionary<Guid, UserConnectionModel> connections = [];
    private static readonly Random random = new();

    private static readonly CoordinatesModel ballPosition = new()
    {
        X = 1000,
        Y = 400
    };

    public static async Task Main(string[] args) => await Initialize();

    private static async Task Initialize()
    {
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://gotikaniki69.com:8081/");
        httpListener.Start();
        Console.WriteLine("WebSocket server started at ws://test.gotikaniki69.com:8081/");

        while (true)
        {
            var context = await httpListener.GetContextAsync().ConfigureAwait(false);
            if (context.Request.IsWebSocketRequest)
            {
                _ = HandleConnectionAsync(context).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private static async Task HandleConnectionAsync(HttpListenerContext context)
    {
        WebSocket? webSocket = null;
        try
        {
            var id = Guid.NewGuid();
            webSocket = (await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false)).WebSocket;

            // Extract and parse query string
            var queryString = context.Request.Url?.Query;
            var queryParams = ParseQueryString(queryString);

            if (queryParams.TryGetValue("name", out var name))
            {
                Console.WriteLine($"Connection received with name: {name}");
            }
            if (queryParams.TryGetValue("skinId", out var skinId))
            {
                Console.WriteLine($"Connection received with skin ID: {skinId}");
            }

            _ = connections.TryAdd(id, new UserConnectionModel(webSocket, name!, skinId!));

            var helloMessage = JsonSerializer.Serialize(new ResponseModel { Type = nameof(MessageTypeEnum.Hello) }, JsonContext.Default.ResponseModel);
            await SendAsync(webSocket, helloMessage).ConfigureAwait(false);

            var ballMovementResponse = JsonSerializer.Serialize(new ResponseModel
            {
                Type = nameof(MessageTypeEnum.BallMovement),
                Payload = new PayloadModel
                {
                    X = ballPosition.X,
                    Y = ballPosition.Y
                }
            }, JsonContext.Default.ResponseModel);
            await SendAsync(webSocket, ballMovementResponse).ConfigureAwait(false);

            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new ArraySegment<byte>(new byte[2048]);
                var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    await ProcessMessageAsync(id, buffer, result).ConfigureAwait(false);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
        finally
        {
            if (webSocket != null)
            {
                foreach (var connection in connections)
                {
                    if (connection.Value.WebSocket == webSocket)
                    {
                        _ = connections.TryRemove(connection.Key, out _);
                        Console.WriteLine($"Active after remove: {connections.Count}");
                        break;
                    }
                }
                webSocket.Dispose();
            }
        }
    }

    // TODO: maybe merge sending ball movement and hit
    private static async Task ProcessMessageAsync(Guid id, ArraySegment<byte> buffer, WebSocketReceiveResult result)
    {
        var message = Encoding.UTF8.GetString(buffer.Array ?? [], buffer.Offset, result.Count);
        Console.WriteLine($"Received: {message}");

        if (!connections.TryGetValue(id, out var sender))
        {
            Console.WriteLine("Sender not found!");
            return;
        }

        var msg = JsonSerializer.Deserialize(message, JsonContext.Default.CoordinatesModel);
        var payload = new PayloadModel
        {
            Index = random.Next(1, 7),
            NickName = $"{sender.NickName} ({sender.Score})",
            SkinId = sender.SkinId,
            X = Math.Max(0, msg?.X ?? 0),
            Y = Math.Max(0, msg?.Y ?? 0)
        };
        var distance = Math.Sqrt(Math.Pow(ballPosition.X - payload.X, 2) + Math.Pow(ballPosition.Y - payload.Y, 2));
        if (distance < 100)
        {
            var force = random.Next(20, 60);
            var newBallPosition = new CoordinatesModel
            {
                X = ballPosition.X + ((ballPosition.X - payload.X) / distance * force),
                Y = ballPosition.Y + ((ballPosition.Y - payload.Y) / distance * force),
            };
            // out of the pitch
            if (newBallPosition.X < 0 || newBallPosition.Y < 0 || newBallPosition.Y > 800 || newBallPosition.X > 1600)
            {
                newBallPosition.X = random.Next(600, 1200);
                newBallPosition.Y = random.Next(300, 750);
            }
            // goal score
            if (newBallPosition.X > 100 && newBallPosition.X < 150 && newBallPosition.Y > 300 && newBallPosition.Y < 500)
            {
                newBallPosition.X = random.Next(600, 1200);
                newBallPosition.Y = random.Next(300, 750);
                payload.Index = 8;
                sender.Score++;
                payload.NickName = sender.NickName + " (" + sender.Score.ToString() + ")";
            }
            var ballMovementResponse = JsonSerializer.Serialize(new ResponseModel
            {
                Type = nameof(MessageTypeEnum.BallMovement),
                Payload = new PayloadModel
                {
                    X = newBallPosition.X,
                    Y = newBallPosition.Y
                }
            }, JsonContext.Default.ResponseModel);
            ballPosition.X = newBallPosition.X;
            ballPosition.Y = newBallPosition.Y;

            Console.WriteLine($"Sended: {ballMovementResponse}");
            Console.WriteLine($"Active connections: {connections.Count}");

            foreach (var connection in connections.Values)
            {
                if (connection.WebSocket.State == WebSocketState.Open)
                {
                    await SendAsync(connection.WebSocket, ballMovementResponse).ConfigureAwait(false);
                }
            }
        }

        var response = JsonSerializer.Serialize(new ResponseModel
        {
            Type = nameof(MessageTypeEnum.Hit),
            Payload = payload
        }, JsonContext.Default.ResponseModel);

        Console.WriteLine($"Sended: {response}");
        Console.WriteLine($"Active connections: {connections.Count}");

        foreach (var connection in connections.Values)
        {
            if (connection.WebSocket.State == WebSocketState.Open)
            {
                await SendAsync(connection.WebSocket, response).ConfigureAwait(false);
            }
        }
    }

    private static async Task SendAsync(WebSocket webSocket, string data)
    {
        var buffer = Encoding.UTF8.GetBytes(data);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
    }

    private static Dictionary<string, string> ParseQueryString(string? queryString)
    {
        var queryParameters = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(queryString) && queryString.StartsWith('?'))
        {
            queryString = queryString.Substring(1); // Remove '?' at the beginning
            foreach (var pair in queryString.Split('&'))
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    var key = WebUtility.UrlDecode(parts[0]);
                    queryParameters[key] = WebUtility.UrlDecode(parts[1]);
                }
            }
        }
        return queryParameters;
    }
}