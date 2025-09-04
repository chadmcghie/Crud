using App.Abstractions;
using Domain.Entities;

namespace App.Services;

public class WindowService(IWindowRepository windows) : IWindowService
{
    public async Task<Window?> GetAsync(Guid id, CancellationToken ct = default)
        => await windows.GetAsync(id, ct);

    public async Task<IReadOnlyList<Window>> ListAsync(CancellationToken ct = default)
        => await windows.ListAsync(ct);

    public async Task<Window> CreateAsync(string name, string? description, double width, double height, double area, string frameType, string? frameDetails, string glazingType, string? glazingDetails, double? uValue = null, double? solarHeatGainCoefficient = null, double? visibleTransmittance = null, double? airLeakage = null, string? energyStarRating = null, string? nfrcRating = null, string? orientation = null, string? location = null, string? installationType = null, string? operationType = null, bool? hasScreens = null, bool? hasStormWindows = null, CancellationToken ct = default)
    {
        var window = new Window
        {
            Name = name,
            Description = description,
            Width = width,
            Height = height,
            Area = area,
            FrameType = frameType,
            FrameDetails = frameDetails,
            GlazingType = glazingType,
            GlazingDetails = glazingDetails,
            UValue = uValue,
            SolarHeatGainCoefficient = solarHeatGainCoefficient,
            VisibleTransmittance = visibleTransmittance,
            AirLeakage = airLeakage,
            EnergyStarRating = energyStarRating,
            NFRCRating = nfrcRating,
            Orientation = orientation,
            Location = location,
            InstallationType = installationType,
            OperationType = operationType,
            HasScreens = hasScreens,
            HasStormWindows = hasStormWindows
        };
        return await windows.AddAsync(window, ct);
    }

    public async Task UpdateAsync(Guid id, string name, string? description, double width, double height, double area, string frameType, string? frameDetails, string glazingType, string? glazingDetails, double? uValue = null, double? solarHeatGainCoefficient = null, double? visibleTransmittance = null, double? airLeakage = null, string? energyStarRating = null, string? nfrcRating = null, string? orientation = null, string? location = null, string? installationType = null, string? operationType = null, bool? hasScreens = null, bool? hasStormWindows = null, CancellationToken ct = default)
    {
        var window = await windows.GetAsync(id, ct) ?? throw new KeyNotFoundException($"Window {id} not found");

        window.Name = name;
        window.Description = description;
        window.Width = width;
        window.Height = height;
        window.Area = area;
        window.FrameType = frameType;
        window.FrameDetails = frameDetails;
        window.GlazingType = glazingType;
        window.GlazingDetails = glazingDetails;
        window.UValue = uValue;
        window.SolarHeatGainCoefficient = solarHeatGainCoefficient;
        window.VisibleTransmittance = visibleTransmittance;
        window.AirLeakage = airLeakage;
        window.EnergyStarRating = energyStarRating;
        window.NFRCRating = nfrcRating;
        window.Orientation = orientation;
        window.Location = location;
        window.InstallationType = installationType;
        window.OperationType = operationType;
        window.HasScreens = hasScreens;
        window.HasStormWindows = hasStormWindows;
        window.UpdatedAt = DateTime.UtcNow;

        await windows.UpdateAsync(window, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await windows.DeleteAsync(id, ct);
}
