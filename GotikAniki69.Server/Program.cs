using GotikAniki69.Server.Game;
using GotikAniki69.Server.Web;
using Microsoft.AspNetCore.ResponseCompression;

namespace GotikAniki69.Server;

public static class Program
{
    private static readonly string[] second =
    [
        "application/javascript",
        "text/javascript",
        "text/css",
        "application/json",
        "application/manifest+json",
        "image/svg+xml"
    ];

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.WebHost.UseUrls("http://0.0.0.0:8081");

        builder.Services.AddSingleton<GameState>();

        builder.Services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true;
            o.Providers.Add<BrotliCompressionProvider>();
            o.Providers.Add<GzipCompressionProvider>();

            o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(second);
        });

        var app = builder.Build();

        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(30)
        });

        app.UseEmbeddedStaticFiles();

        app.MapGameWebSocket("/ws");

        await app.RunAsync();
    }
}
