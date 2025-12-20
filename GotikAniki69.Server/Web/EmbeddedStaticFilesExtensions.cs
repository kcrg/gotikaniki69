using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace GotikAniki69.Server.Web;

public static class EmbeddedStaticFilesExtensions
{
    public static IApplicationBuilder UseEmbeddedStaticFiles(this WebApplication app)
    {
        var asm = Assembly.GetExecutingAssembly();
        var embedded = new ManifestEmbeddedFileProvider(asm, "wwwroot");

        var ctp = new FileExtensionContentTypeProvider();
        ctp.Mappings[".mp3"] = "audio/mpeg";
        ctp.Mappings[".gif"] = "image/gif";
        ctp.Mappings[".ttf"] = "font/ttf";
        ctp.Mappings[".webmanifest"] = "application/manifest+json";

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embedded,
            ContentTypeProvider = ctp,
            OnPrepareResponse = ctx =>
            {
                var path = ctx.Context.Request.Path.Value ?? "";
                if (path.Equals("/", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("index.html", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=60";
                }
                else
                {
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
                }
            }
        });

        app.MapGet("/", async (HttpContext ctx) =>
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            var file = embedded.GetFileInfo("index.html");
            await ctx.Response.SendFileAsync(file);
        });

        return app;
    }
}
