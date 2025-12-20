using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace GotikAniki69.Server.Web;

public static class EmbeddedStaticFilesExtensions
{
    public static WebApplication UseEmbeddedStaticFiles(this WebApplication app)
    {
        // kompresja dla HTML/JS/CSS/manifest/json
        app.UseResponseCompression();

        var asm = Assembly.GetExecutingAssembly();
        var embedded = new ManifestEmbeddedFileProvider(asm, "wwwroot");

        var ctp = new FileExtensionContentTypeProvider();
        ctp.Mappings[".mp3"] = "audio/mpeg";
        ctp.Mappings[".gif"] = "image/gif";
        ctp.Mappings[".ttf"] = "font/ttf";
        ctp.Mappings[".webmanifest"] = "application/manifest+json";
        ctp.Mappings[".js"] = "text/javascript; charset=utf-8";
        ctp.Mappings[".css"] = "text/css; charset=utf-8";

        var appEtagSeed = asm.ManifestModule.ModuleVersionId.ToString("N"); // zmienia się przy rebuildzie

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embedded,
            ContentTypeProvider = ctp,
            OnPrepareResponse = ctx =>
            {
                var http = ctx.Context;
                var path = http.Request.Path.Value ?? "";

                var isIndex =
                    path is "/" ||
                    path.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("index.html", StringComparison.OrdinalIgnoreCase);

                var isScript =
                    path.EndsWith("/script.js", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("script.js", StringComparison.OrdinalIgnoreCase);

                // entrypointy (index + script) zawsze rewaliduj
                http.Response.Headers.CacheControl = (isIndex || isScript)
                    ? (StringValues)"public,max-age=0,must-revalidate"
                    : (StringValues)"public,max-age=31536000,immutable";

                var fi = ctx.File;
                if (fi?.Exists != true)
                    return;

                var etag = ComputeEtag(appEtagSeed, fi.Length, fi.LastModified.UtcDateTime);
                http.Response.Headers.ETag = etag;

                if (http.Request.Headers.TryGetValue("If-None-Match", out var inm) && inm == etag)
                {
                    http.Response.StatusCode = StatusCodes.Status304NotModified;
                    http.Response.ContentLength = 0;
                    http.Response.Body = Stream.Null;
                }
            }
        });

        // root -> index.html (również z ETag/must-revalidate przez middleware)
        app.MapGet("/", async ctx =>
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.SendFileAsync(embedded.GetFileInfo("index.html"));
        });

        return app;
    }

    // mocny ETag zależny od wersji binarki + metadanych pliku
    private static string ComputeEtag(string seed, long length, DateTime lastModifiedUtc)
    {
        Span<byte> hash = stackalloc byte[32];

        var s = $"{seed}:{length}:{lastModifiedUtc.Ticks}";
        var bytes = Encoding.UTF8.GetBytes(s);

        SHA256.HashData(bytes, hash);
        // ETag w cudzysłowie
        return $"\"{Convert.ToHexString(hash)}\"";
    }
}
