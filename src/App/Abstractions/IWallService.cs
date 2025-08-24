using Domain.Entities;

namespace App.Abstractions;

public interface IWallService
{
    Task<Wall?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Wall>> ListAsync(CancellationToken ct = default);
    Task<Wall> CreateAsync(string name, string? description, double length, double height, double thickness, string assemblyType, string? assemblyDetails = null, double? rValue = null, double? uValue = null, string? materialLayers = null, string? orientation = null, string? location = null, CancellationToken ct = default);
    Task UpdateAsync(Guid id, string name, string? description, double length, double height, double thickness, string assemblyType, string? assemblyDetails = null, double? rValue = null, double? uValue = null, string? materialLayers = null, string? orientation = null, string? location = null, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
