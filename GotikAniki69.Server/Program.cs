using GotikAniki69.Server.Game;
using GotikAniki69.Server.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GotikAniki69.Server;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.WebHost.UseUrls("http://0.0.0.0:8081");

        builder.Services.AddSingleton<GameState>();

        var app = builder.Build();

        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(30)
        });

        // statyki z EXE (embedded wwwroot) + "/" => index.html
        app.UseEmbeddedStaticFiles();

        // websocket endpoint 1:1 jak u Ciebie: /ws?name=...&skinId=...
        app.MapGameWebSocket("/ws");

        await app.RunAsync();
    }
}
