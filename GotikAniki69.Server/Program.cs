using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GotikAniki69.Server.Models;

namespace GotikAniki69.Server;

public static class Program
{
    private static readonly ConcurrentDictionary<Guid, WebSocket> connections = [];
    private const string HelloMessage = "{\"type\":\"hello\"}";
    private static readonly Random random = new();

    public static async Task Main(string[] args)
    {
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://gotikaniki69.com:8081/");
        httpListener.Start();
        Console.WriteLine("WebSocket server started at ws://gotikaniki69.com:8081/");

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

            _ = connections.TryAdd(id, webSocket);

            await SendAsync(webSocket, HelloMessage).ConfigureAwait(false);

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
                    await ProcessMessageAsync(buffer, result).ConfigureAwait(false);
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
                foreach (var pair in connections)
                {
                    if (pair.Value == webSocket)
                    {
                        _ = connections.TryRemove(pair.Key, out _);
                        break;
                    }
                }
                webSocket.Dispose();
            }
        }
    }

    private static async Task ProcessMessageAsync(ArraySegment<byte> buffer, WebSocketReceiveResult result)
    {
        var message = Encoding.UTF8.GetString(buffer.Array ?? [], buffer.Offset, result.Count);
        Console.WriteLine($"Received: {message}");

        var msg = JsonSerializer.Deserialize(message, JsonContext.Default.Coordinates);
        var response = JsonSerializer.Serialize(new Response
        {
            Type = "hit",
            Payload = new Payload
            {
                X = Math.Max(0, msg?.X ?? 0),
                Y = Math.Max(0, msg?.Y ?? 0),
                Index = random.Next(1, 7)
            }
        }, JsonContext.Default.Response);

        foreach (var socket in connections.Values)
        {
            if (socket.State == WebSocketState.Open)
            {
                await SendAsync(socket, response).ConfigureAwait(false);
            }
        }
    }

    private static async Task SendAsync(WebSocket webSocket, string data)
    {
        var buffer = Encoding.UTF8.GetBytes(data);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
    }
}