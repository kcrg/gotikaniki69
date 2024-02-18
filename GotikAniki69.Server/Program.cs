using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GotikAniki69.Server.Models;

namespace GotikAniki69.Server;

internal static class Program
{
    private static readonly List<WebSocket> connections = [];

    private static async Task Main(string[] args)
    {
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://gotikaniki69.com:8081/");
        httpListener.Start();
        Console.WriteLine("WebSocket server started at ws://gotikaniki69.com:8081/");

        while (true)
        {
            var context = await httpListener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                _ = HandleConnection(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private static async Task HandleConnection(HttpListenerContext context)
    {
        var webSocket = (await context.AcceptWebSocketAsync(subProtocol: null)).WebSocket;
        connections.Add(webSocket);
        await SendAsync(webSocket, "{\"type\":\"hello\"}");

        while (webSocket.State == WebSocketState.Open)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            try
            {
                var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                    Console.WriteLine($"Received: {message}");

                    var msg = JsonSerializer.Deserialize(message, JsonContext.Default.Coordinates);
                    var response = JsonSerializer.Serialize(new Response
                    {
                        Type = "hit",
                        Payload = new Payload
                        {
                            X = Math.Max(0, msg?.X ?? 0),
                            Y = Math.Max(0, msg?.Y ?? 0),
                            Index = new Random().Next(1, 8)
                        }
                    }, JsonContext.Default.Response);

                    foreach (var socket in connections)
                    {
                        if (socket.State == WebSocketState.Open)
                        {
                            await SendAsync(socket, response);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.StackTrace);
                break; // Exit loop on error
            }
        }

        _ = connections.Remove(webSocket);
    }

    private static async Task SendAsync(WebSocket webSocket, string data)
    {
        var buffer = Encoding.UTF8.GetBytes(data);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}