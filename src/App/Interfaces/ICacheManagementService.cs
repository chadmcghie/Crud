using System.Threading;
using System.Threading.Tasks;

namespace App.Interfaces;

public interface ICacheManagementService
{
    Task ClearAllAsync(CancellationToken cancellationToken = default);
    Task ClearByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task RemoveKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default);
    Task WarmCriticalDataAsync(CancellationToken cancellationToken = default);
    Task<long> GetKeyCountAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", int limit = 100, CancellationToken cancellationToken = default);
}
