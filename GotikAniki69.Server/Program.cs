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

            // Extract and parse query string
            var queryString = context.Request.Url?.Query;
            var queryParams = ParseQueryString(queryString);

            // Example use: Print the 'name' query parameter
            if (queryParams.TryGetValue("name", out var name))
            {
                Console.WriteLine($"Connection received with name: {name}");
            }

            _ = connections.TryAdd(id, new UserConnectionModel(webSocket, name!));

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
                        break;
                    }
                }
                webSocket.Dispose();
            }
        }
    }

    private static async Task ProcessMessageAsync(Guid id, ArraySegment<byte> buffer, WebSocketReceiveResult result)
    {
        var message = Encoding.UTF8.GetString(buffer.Array ?? [], buffer.Offset, result.Count);
        Console.WriteLine($"Received: {message}");

        var name = connections.TryGetValue(id, out var sender) ? sender.UserName : string.Empty;

        var msg = JsonSerializer.Deserialize(message, JsonContext.Default.Coordinates);
        var response = JsonSerializer.Serialize(new Response
        {
            Type = "hit",
            Payload = new Payload
            {
                Index = random.Next(1, 7),
                Nick = name,
                X = Math.Max(0, msg?.X ?? 0),
                Y = Math.Max(0, msg?.Y ?? 0)
            }
        }, JsonContext.Default.Response);

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