using Domain.Entities;

namespace App.Abstractions;

public interface IWindowService
{
    Task<Window?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Window>> ListAsync(CancellationToken ct = default);
    Task<Window> CreateAsync(string name, string? description, double width, double height, double area, string frameType, string? frameDetails, string glazingType, string? glazingDetails, double? uValue = null, double? solarHeatGainCoefficient = null, double? visibleTransmittance = null, double? airLeakage = null, string? energyStarRating = null, string? nfrcRating = null, string? orientation = null, string? location = null, string? installationType = null, string? operationType = null, bool? hasScreens = null, bool? hasStormWindows = null, CancellationToken ct = default);
    Task UpdateAsync(Guid id, string name, string? description, double width, double height, double area, string frameType, string? frameDetails, string glazingType, string? glazingDetails, double? uValue = null, double? solarHeatGainCoefficient = null, double? visibleTransmittance = null, double? airLeakage = null, string? energyStarRating = null, string? nfrcRating = null, string? orientation = null, string? location = null, string? installationType = null, string? operationType = null, bool? hasScreens = null, bool? hasStormWindows = null, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
