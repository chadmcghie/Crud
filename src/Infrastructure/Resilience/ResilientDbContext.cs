using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Resilience
{
    public interface IResilientDbContext
    {
        Task<int> SaveChangesWithRetryAsync(CancellationToken cancellationToken = default);
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
        Task ExecuteWithRetryAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    }

    public class ResilientDbContextWrapper<TContext> : IResilientDbContext where TContext : DbContext
    {
        private readonly TContext _context;
        private readonly IAsyncPolicy _retryPolicy;
        private readonly ILogger<ResilientDbContextWrapper<TContext>> _logger;

        public ResilientDbContextWrapper(
            TContext context,
            ILogger<ResilientDbContextWrapper<TContext>> logger)
        {
            _context = context;
            _logger = logger;
            _retryPolicy = PollyPolicies.GetComprehensiveDatabasePolicy(logger);
        }

        public async Task<int> SaveChangesWithRetryAsync(CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _context.SaveChangesAsync(cancellationToken);
            });
        }

        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            });
        }

        public async Task ExecuteWithRetryAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await operation();
            });
        }
    }

    /// <summary>
    /// Extension methods for adding resilience to DbContext operations
    /// </summary>
    public static class DbContextResilienceExtensions
    {
        public static async Task<int> SaveChangesWithRetryAsync(
            this DbContext context,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var policy = PollyPolicies.GetComprehensiveDatabasePolicy(logger);
            return await policy.ExecuteAsync(async () =>
            {
                return await context.SaveChangesAsync(cancellationToken);
            });
        }

        public static async Task<T> ExecuteWithRetryAsync<T>(
            this DbContext context,
            Func<Task<T>> operation,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var policy = PollyPolicies.GetComprehensiveDatabasePolicy(logger);
            return await policy.ExecuteAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            });
        }

        public static async Task<T> ExecuteWithBulkheadAsync<T>(
            this DbContext context,
            Func<Task<T>> operation,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            var bulkheadPolicy = PollyPolicies.GetDatabaseBulkheadPolicy(logger);
            var comprehensivePolicy = PollyPolicies.GetComprehensiveDatabasePolicy(logger);
            
            // Combine bulkhead with comprehensive resilience
            var combinedPolicy = Policy.WrapAsync(bulkheadPolicy, comprehensivePolicy);
            
            return await combinedPolicy.ExecuteAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            });
        }
    }
}