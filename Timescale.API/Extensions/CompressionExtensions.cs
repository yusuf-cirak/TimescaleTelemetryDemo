using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace Timescale.API.Extensions;

public static class CompressionExtensions
{
    public static void AddCompression(this WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(opt =>
        {
            opt.Providers.Add<GzipCompressionProvider>();
            opt.Providers.Add<BrotliCompressionProvider>();
        });

        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });
    }

    public static void UseCompression(this WebApplication app)
    {
        app.UseResponseCompression();
    }
}