using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Infrastructure.Resilience
{
    public static class PollyPolicies
    {
        /// <summary>
        /// Configures HTTP retry policy with exponential backoff
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetService<ILogger<HttpMessageHandler>>();

            return HttpPolicyExtensions
                .HandleTransientHttpError() // Handles HttpRequestException, 5XX, and 408
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    5, // Number of retries
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        logger?.LogWarning("HTTP request retry {RetryCount} after {TimeSpan}ms delay. Status: {StatusCode}",
                            retryCount, timespan.TotalMilliseconds, outcome.Result?.StatusCode);
                    });
        }

        /// <summary>
        /// Configures HTTP circuit breaker policy
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetHttpCircuitBreakerPolicy(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetService<ILogger<HttpMessageHandler>>();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    3, // Number of consecutive failures before opening circuit
                    TimeSpan.FromSeconds(30), // Duration of open state
                    onBreak: (result, timespan) =>
                    {
                        logger?.LogError("Circuit breaker opened for {TimeSpan}s due to consecutive failures",
                            timespan.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        logger?.LogInformation("Circuit breaker reset, normal operation resumed");
                    },
                    onHalfOpen: () =>
                    {
                        logger?.LogInformation("Circuit breaker is half-open, testing with next request");
                    });
        }

        /// <summary>
        /// Combines retry and circuit breaker policies for HTTP
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCombinedHttpPolicy(IServiceProvider serviceProvider)
        {
            var retryPolicy = GetHttpRetryPolicy(serviceProvider);
            var circuitBreakerPolicy = GetHttpCircuitBreakerPolicy(serviceProvider);

            // Wrap retry around circuit breaker
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        /// <summary>
        /// Database retry policy with exponential backoff
        /// </summary>
        public static IAsyncPolicy GetDatabaseRetryPolicy(ILogger logger = null)
        {
            return Policy
                .Handle<Exception>(ex =>
                {
                    // Retry on transient database errors
                    var message = ex.Message?.ToLower() ?? "";
                    return message.Contains("timeout") ||
                           message.Contains("deadlock") ||
                           message.Contains("connection") ||
                           message.Contains("transient") ||
                           message.Contains("busy") || // SQLite busy
                           message.Contains("locked"); // SQLite locked
                })
                .WaitAndRetryAsync(
                    3, // Number of retries
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        logger?.LogWarning(exception,
                            "Database operation retry {RetryCount} after {TimeSpan}ms delay",
                            retryCount, timespan.TotalMilliseconds);
                    });
        }

        /// <summary>
        /// Database circuit breaker policy
        /// </summary>
        public static IAsyncPolicy GetDatabaseCircuitBreakerPolicy(ILogger logger = null)
        {
            return Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    5, // Number of consecutive failures before opening circuit
                    TimeSpan.FromSeconds(60), // Duration of open state
                    onBreak: (exception, timespan) =>
                    {
                        logger?.LogError(exception,
                            "Database circuit breaker opened for {TimeSpan}s due to consecutive failures",
                            timespan.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        logger?.LogInformation("Database circuit breaker reset, normal operation resumed");
                    },
                    onHalfOpen: () =>
                    {
                        logger?.LogInformation("Database circuit breaker is half-open, testing with next request");
                    });
        }

        /// <summary>
        /// Combines retry and circuit breaker policies for database operations
        /// </summary>
        public static IAsyncPolicy GetCombinedDatabasePolicy(ILogger logger = null)
        {
            var retryPolicy = GetDatabaseRetryPolicy(logger);
            var circuitBreakerPolicy = GetDatabaseCircuitBreakerPolicy(logger);

            // Wrap retry around circuit breaker
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        /// <summary>
        /// Test-specific retry policy with faster retries
        /// </summary>
        public static IAsyncPolicy GetTestRetryPolicy(ILogger logger = null)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    5, // More retries for tests
                    retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt), // Faster retries
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        logger?.LogDebug(exception,
                            "Test operation retry {RetryCount} after {TimeSpan}ms",
                            retryCount, timespan.TotalMilliseconds);
                    });
        }
    }
}