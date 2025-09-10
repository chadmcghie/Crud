using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace Api.Middleware;

public class CompressionPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CompressionPerformanceMiddleware> _logger;
    private static readonly Meter _meter = new("Crud.Api.Compression");
    private static readonly Histogram<double> _compressionRatioHistogram = _meter.CreateHistogram<double>(
        "compression.ratio",
        "percent",
        "The compression ratio achieved for responses");
    private static readonly Histogram<long> _originalSizeHistogram = _meter.CreateHistogram<long>(
        "compression.original_size",
        "bytes",
        "The original response size before compression");
    private static readonly Histogram<long> _compressedSizeHistogram = _meter.CreateHistogram<long>(
        "compression.compressed_size",
        "bytes",
        "The compressed response size");
    private static readonly Counter<long> _compressionCounter = _meter.CreateCounter<long>(
        "compression.requests_compressed",
        "requests",
        "Number of requests that were compressed");

    public CompressionPerformanceMiddleware(RequestDelegate next, ILogger<CompressionPerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await _next(context);

        var originalSize = responseBodyStream.Length;
        responseBodyStream.Seek(0, SeekOrigin.Begin);

        // Check if response was compressed
        var contentEncoding = context.Response.Headers.ContentEncoding.FirstOrDefault();
        var wasCompressed = !string.IsNullOrEmpty(contentEncoding) &&
                           (contentEncoding.Contains("gzip") || contentEncoding.Contains("br"));

        if (wasCompressed && originalSize > 0 && !string.IsNullOrEmpty(contentEncoding))
        {
            // For compressed responses, we need to estimate the original uncompressed size
            // This is an approximation since we don't have the actual uncompressed size
            var compressionRatio = EstimateCompressionRatio(contentEncoding, context.Response.ContentType);
            var estimatedOriginalSize = (long)(originalSize / (1.0 - compressionRatio / 100.0));

            var compressionPercent = Math.Round(compressionRatio, 2);

            _compressionRatioHistogram.Record(compressionPercent, new KeyValuePair<string, object?>("content_type", context.Response.ContentType ?? "unknown"));
            _originalSizeHistogram.Record(estimatedOriginalSize, new KeyValuePair<string, object?>("content_type", context.Response.ContentType ?? "unknown"));
            _compressedSizeHistogram.Record(originalSize, new KeyValuePair<string, object?>("content_type", context.Response.ContentType ?? "unknown"));
            _compressionCounter.Add(1, new KeyValuePair<string, object?>("encoding", contentEncoding));

            _logger.LogInformation(
                "Response compressed: {ContentType} | Original: ~{EstimatedOriginalSize} bytes | Compressed: {CompressedSize} bytes | Ratio: {CompressionPercent}% | Encoding: {ContentEncoding}",
                context.Response.ContentType ?? "unknown",
                estimatedOriginalSize,
                originalSize,
                compressionPercent,
                contentEncoding);
        }
        else if (originalSize > 1000) // Only log large uncompressed responses
        {
            _logger.LogDebug(
                "Response not compressed: {ContentType} | Size: {Size} bytes | Path: {Path}",
                context.Response.ContentType ?? "unknown",
                originalSize,
                context.Request.Path);
        }

        // Copy the response back to the original stream
        context.Response.Body = originalBodyStream;
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalBodyStream);
    }

    private static double EstimateCompressionRatio(string encoding, string? contentType)
    {
        // Typical compression ratios based on content type and encoding
        var baseRatio = contentType?.ToLower() switch
        {
            var ct when ct.Contains("json") => 75.0,
            var ct when ct.Contains("html") => 70.0,
            var ct when ct.Contains("css") => 80.0,
            var ct when ct.Contains("javascript") => 65.0,
            var ct when ct.Contains("xml") => 70.0,
            var ct when ct.Contains("text") => 60.0,
            _ => 50.0
        };

        // Brotli typically achieves 10-20% better compression than gzip
        return encoding.Contains("br") ? Math.Min(baseRatio + 10.0, 85.0) : baseRatio;
    }
}
