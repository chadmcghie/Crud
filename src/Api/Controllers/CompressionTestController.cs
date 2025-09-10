using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompressionTestController : ControllerBase
{
    private readonly ILogger<CompressionTestController> _logger;

    public CompressionTestController(ILogger<CompressionTestController> logger)
    {
        _logger = logger;
    }

    [HttpGet("large-json")]
    public ActionResult<object> GetLargeJson()
    {
        // Generate a large JSON response with repetitive data (compresses well)
        var largeData = new
        {
            Message = "This is a test message to evaluate compression effectiveness. " +
                     "This message will be repeated many times to create a large response that should compress well. " +
                     "JSON compression typically achieves 70-80% size reduction on repetitive data like this.",
            Timestamp = DateTime.UtcNow,
            Data = Enumerable.Range(1, 500).Select(i => new
            {
                Id = i,
                Name = $"Test Item {i}",
                Description = "This is a detailed description of a test item that contains repetitive text patterns " +
                             "which should compress very well using gzip or brotli compression algorithms. " +
                             "The more repetitive content we have, the better the compression ratio will be.",
                Category = (i % 10) switch
                {
                    0 => "Electronics",
                    1 => "Clothing",
                    2 => "Books",
                    3 => "Home & Garden",
                    4 => "Sports",
                    5 => "Automotive",
                    6 => "Health & Beauty",
                    7 => "Toys & Games",
                    8 => "Food & Beverages",
                    _ => "Miscellaneous"
                },
                Price = Math.Round(19.99 + (i * 0.99), 2),
                InStock = i % 3 != 0,
                Tags = new[] { "test", "compression", "demo", $"category-{i % 10}", "sample-data" },
                Metadata = new
                {
                    CreatedDate = DateTime.UtcNow.AddDays(-i),
                    LastModified = DateTime.UtcNow.AddHours(-i),
                    Version = "1.0.0",
                    Source = "CompressionTestController"
                }
            }).ToArray(),
            Statistics = new
            {
                TotalItems = 500,
                EstimatedUncompressedSize = "~50KB",
                ExpectedCompressionRatio = "70-80%",
                SupportedEncodings = new[] { "gzip", "br" }
            }
        };

        _logger.LogInformation("Generated large JSON response with {ItemCount} items for compression testing", 500);
        return Ok(largeData);
    }

    [HttpGet("large-text")]
    public ActionResult<string> GetLargeText()
    {
        var sb = new StringBuilder();

        // Generate repetitive text content
        var baseText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                      "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                      "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris. ";

        for (int i = 0; i < 1000; i++)
        {
            sb.AppendLine($"Line {i + 1}: {baseText}");
            if (i % 50 == 0)
            {
                sb.AppendLine("--- Section Break ---");
            }
        }

        var content = sb.ToString();
        _logger.LogInformation("Generated large text response of {Size} characters for compression testing", content.Length);

        return Content(content, "text/plain");
    }

    [HttpGet("mixed-content")]
    public ActionResult<object> GetMixedContent()
    {
        var mixedData = new
        {
            Html = """
                   <!DOCTYPE html>
                   <html lang="en">
                   <head>
                       <meta charset="UTF-8">
                       <meta name="viewport" content="width=device-width, initial-scale=1.0">
                       <title>Compression Test Page</title>
                       <style>
                           body { font-family: Arial, sans-serif; margin: 20px; }
                           .container { max-width: 800px; margin: 0 auto; }
                           .section { margin-bottom: 20px; padding: 15px; border: 1px solid #ddd; }
                           h1, h2, h3 { color: #333; }
                           p { line-height: 1.6; color: #666; }
                           .highlight { background-color: #fff3cd; padding: 10px; border-radius: 5px; }
                       </style>
                   </head>
                   <body>
                       <div class="container">
                           <h1>Compression Effectiveness Test</h1>
                           <div class="section">
                               <h2>About This Test</h2>
                               <p>This HTML content is designed to test compression effectiveness. It contains repetitive elements and structured markup that should compress well.</p>
                           </div>
                   """,
            Css = """
                  .test-class { display: flex; flex-direction: column; align-items: center; }
                  .another-test-class { background-color: #f8f9fa; border-radius: 8px; padding: 16px; }
                  .repeated-styles { margin: 8px; padding: 8px; border: 1px solid #dee2e6; }
                  """,
            JavaScript = """
                         function testCompressionFunction() {
                             console.log('Testing compression effectiveness');
                             const data = Array.from({length: 100}, (_, i) => ({
                                 id: i,
                                 name: `Item ${i}`,
                                 value: Math.random() * 100
                             }));
                             return data;
                         }
                         
                         function anotherTestFunction() {
                             const repetitiveData = 'This is repetitive data that should compress well. '.repeat(50);
                             return repetitiveData;
                         }
                         """,
            JsonData = Enumerable.Range(1, 100).Select(i => new
            {
                Id = i,
                Title = $"Test Article {i}",
                Content = "This is sample content for compression testing. " +
                         "The content contains repetitive patterns and common phrases " +
                         "that should result in good compression ratios when using gzip or brotli.",
                Tags = new[] { "test", "compression", "sample" },
                PublishedDate = DateTime.UtcNow.AddDays(-i)
            }).ToArray()
        };

        _logger.LogInformation("Generated mixed content response for compression testing");
        return Ok(mixedData);
    }

    [HttpGet("compression-info")]
    public ActionResult<object> GetCompressionInfo()
    {
        var acceptEncoding = Request.Headers.AcceptEncoding.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var info = new
        {
            RequestInfo = new
            {
                AcceptEncoding = acceptEncoding,
                UserAgent = userAgent,
                SupportsGzip = acceptEncoding.Contains("gzip"),
                SupportsBrotli = acceptEncoding.Contains("br"),
                Timestamp = DateTime.UtcNow
            },
            CompressionSettings = new
            {
                Message = "This endpoint provides information about compression support and settings.",
                ExpectedBehavior = "Responses should be compressed when client supports it and content is compressible.",
                TestEndpoints = new[]
                {
                    "/api/compressiontest/large-json - Large JSON response (~50KB)",
                    "/api/compressiontest/large-text - Large text response (~75KB)",
                    "/api/compressiontest/mixed-content - Mixed HTML/CSS/JS content (~25KB)"
                },
                ExpectedCompressionRatios = new
                {
                    Json = "70-80%",
                    Text = "60-70%",
                    Html = "70-80%",
                    Css = "80-85%",
                    JavaScript = "65-75%"
                }
            }
        };

        return Ok(info);
    }
}
