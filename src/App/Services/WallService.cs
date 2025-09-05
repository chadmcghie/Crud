using App.Abstractions;
using Domain.Entities;

namespace App.Services;

public class WallService(IWallRepository walls) : IWallService
{
    public async Task<Wall?> GetAsync(Guid id, CancellationToken ct = default)
        => await walls.GetAsync(id, ct);

    public async Task<IReadOnlyList<Wall>> ListAsync(CancellationToken ct = default)
        => await walls.ListAsync(ct);

    public async Task<Wall> CreateAsync(string name, string? description, double length, double height, double thickness, string assemblyType, string? assemblyDetails = null, double? rValue = null, double? uValue = null, string? materialLayers = null, string? orientation = null, string? location = null, CancellationToken ct = default)
    {
        var wall = new Wall
        {
            Name = name,
            Description = description,
            Length = length,
            Height = height,
            Thickness = thickness,
            AssemblyType = assemblyType,
            AssemblyDetails = assemblyDetails,
            RValue = rValue,
            UValue = uValue,
            MaterialLayers = materialLayers,
            Orientation = orientation,
            Location = location
        };
        return await walls.AddAsync(wall, ct);
    }

    public async Task UpdateAsync(Guid id, string name, string? description, double length, double height, double thickness, string assemblyType, string? assemblyDetails = null, double? rValue = null, double? uValue = null, string? materialLayers = null, string? orientation = null, string? location = null, CancellationToken ct = default)
    {
        var wall = await walls.GetAsync(id, ct) ?? throw new KeyNotFoundException($"Wall {id} not found");

        wall.Name = name;
        wall.Description = description;
        wall.Length = length;
        wall.Height = height;
        wall.Thickness = thickness;
        wall.AssemblyType = assemblyType;
        wall.AssemblyDetails = assemblyDetails;
        wall.RValue = rValue;
        wall.UValue = uValue;
        wall.MaterialLayers = materialLayers;
        wall.Orientation = orientation;
        wall.Location = location;
        wall.UpdatedAt = DateTime.UtcNow;

        await walls.UpdateAsync(wall, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await walls.DeleteAsync(id, ct);
}
