using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace Api.Configuration;

public static class ResponseCompressionConfiguration
{
    public static IServiceCollection AddResponseCompressionServices(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { 
                    // API responses
                    "application/json",
                    "text/json", 
                    // Static files
                    "text/css",
                    "application/javascript",
                    "text/javascript",
                    "text/html",
                    "application/xml",
                    "text/xml",
                    "text/plain"
                });
        });

        // Configure compression providers
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        return services;
    }
}
