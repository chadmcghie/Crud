using Api.Dtos;
using AutoFixture;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Test data builders for creating consistent test data
/// </summary>
public static class TestDataBuilders
{
    private static readonly Fixture _fixture = new();

    #region Role Builders

    public static CreateRoleRequest CreateRoleRequest(string? name = null, string? description = null)
    {
        return new CreateRoleRequest(
            name ?? _fixture.Create<string>(),
            description
        );
    }

    public static UpdateRoleRequest UpdateRoleRequest(string? name = null, string? description = null)
    {
        return new UpdateRoleRequest(
            name ?? _fixture.Create<string>(),
            description
        );
    }

    #endregion

    #region Person Builders

    public static CreatePersonRequest CreatePersonRequest(
        string? fullName = null, 
        string? phone = null, 
        IEnumerable<Guid>? roleIds = null)
    {
        return new CreatePersonRequest(
            fullName ?? _fixture.Create<string>(),
            phone,
            roleIds ?? Array.Empty<Guid>()
        );
    }

    public static UpdatePersonRequest UpdatePersonRequest(
        string? fullName = null, 
        string? phone = null, 
        IEnumerable<Guid>? roleIds = null)
    {
        return new UpdatePersonRequest(
            fullName ?? _fixture.Create<string>(),
            phone,
            roleIds ?? Array.Empty<Guid>()
        );
    }

    #endregion

    #region Wall Builders

    public static CreateWallRequest CreateWallRequest(
        string? name = null,
        string? description = null,
        double? length = null,
        double? height = null,
        double? thickness = null,
        string? assemblyType = null,
        string? assemblyDetails = null,
        double? rValue = null,
        double? uValue = null,
        string? materialLayers = null,
        string? orientation = null,
        string? location = null)
    {
        return new CreateWallRequest(
            name ?? _fixture.Create<string>(),
            description, // Keep null if passed as null
            length ?? _fixture.Create<double>(),
            height ?? _fixture.Create<double>(),
            thickness ?? _fixture.Create<double>(),
            assemblyType ?? _fixture.Create<string>(), // Required field
            assemblyDetails, // Keep null if passed as null
            rValue, // Keep null if passed as null
            uValue, // Keep null if passed as null
            materialLayers, // Keep null if passed as null
            orientation, // Keep null if passed as null
            location // Keep null if passed as null
        );
    }

    public static UpdateWallRequest UpdateWallRequest(
        string? name = null,
        string? description = null,
        double? length = null,
        double? height = null,
        double? thickness = null,
        string? assemblyType = null,
        string? assemblyDetails = null,
        double? rValue = null,
        double? uValue = null,
        string? materialLayers = null,
        string? orientation = null,
        string? location = null)
    {
        return new UpdateWallRequest(
            name ?? _fixture.Create<string>(),
            description, // Keep null if passed as null
            length ?? _fixture.Create<double>(),
            height ?? _fixture.Create<double>(),
            thickness ?? _fixture.Create<double>(),
            assemblyType ?? _fixture.Create<string>(), // Required field
            assemblyDetails, // Keep null if passed as null
            rValue, // Keep null if passed as null
            uValue, // Keep null if passed as null
            materialLayers, // Keep null if passed as null
            orientation, // Keep null if passed as null
            location // Keep null if passed as null
        );
    }

    #endregion

    #region Window Builders

    public static CreateWindowRequest CreateWindowRequest(
        string? name = null,
        string? description = null,
        double? width = null,
        double? height = null,
        double? area = null,
        string? frameType = null,
        string? frameDetails = null,
        string? glazingType = null,
        string? glazingDetails = null,
        double? uValue = null,
        double? solarHeatGainCoefficient = null,
        double? visibleTransmittance = null,
        double? airLeakage = null,
        string? energyStarRating = null,
        string? nfrcRating = null,
        string? orientation = null,
        string? location = null,
        string? installationType = null,
        string? operationType = null,
        bool? hasScreens = null,
        bool? hasStormWindows = null)
    {
        return new CreateWindowRequest(
            name ?? _fixture.Create<string>(),
            description, // Keep null if passed as null
            width ?? _fixture.Create<double>(),
            height ?? _fixture.Create<double>(),
            area ?? _fixture.Create<double>(),
            frameType ?? "Vinyl", // Required field - use sensible default
            frameDetails, // Keep null if passed as null
            glazingType ?? "Double Pane", // Required field - use sensible default
            glazingDetails, // Keep null if passed as null
            uValue, // Keep null if passed as null
            solarHeatGainCoefficient, // Keep null if passed as null
            visibleTransmittance, // Keep null if passed as null
            airLeakage, // Keep null if passed as null
            energyStarRating, // Keep null if passed as null
            nfrcRating, // Keep null if passed as null
            orientation, // Keep null if passed as null
            location, // Keep null if passed as null
            installationType, // Keep null if passed as null
            operationType, // Keep null if passed as null
            hasScreens, // Keep null if passed as null
            hasStormWindows // Keep null if passed as null
        );
    }

    public static UpdateWindowRequest UpdateWindowRequest(
        string? name = null,
        string? description = null,
        double? width = null,
        double? height = null,
        double? area = null,
        string? frameType = null,
        string? frameDetails = null,
        string? glazingType = null,
        string? glazingDetails = null,
        double? uValue = null,
        double? solarHeatGainCoefficient = null,
        double? visibleTransmittance = null,
        double? airLeakage = null,
        string? energyStarRating = null,
        string? nfrcRating = null,
        string? orientation = null,
        string? location = null,
        string? installationType = null,
        string? operationType = null,
        bool? hasScreens = null,
        bool? hasStormWindows = null)
    {
        return new UpdateWindowRequest(
            name ?? _fixture.Create<string>(),
            description, // Keep null if passed as null
            width ?? _fixture.Create<double>(),
            height ?? _fixture.Create<double>(),
            area ?? _fixture.Create<double>(),
            frameType ?? "Vinyl", // Required field - use sensible default
            frameDetails, // Keep null if passed as null
            glazingType ?? "Double Pane", // Required field - use sensible default
            glazingDetails, // Keep null if passed as null
            uValue, // Keep null if passed as null
            solarHeatGainCoefficient, // Keep null if passed as null
            visibleTransmittance, // Keep null if passed as null
            airLeakage, // Keep null if passed as null
            energyStarRating, // Keep null if passed as null
            nfrcRating, // Keep null if passed as null
            orientation, // Keep null if passed as null
            location, // Keep null if passed as null
            installationType, // Keep null if passed as null
            operationType, // Keep null if passed as null
            hasScreens, // Keep null if passed as null
            hasStormWindows // Keep null if passed as null
        );
    }

    #endregion

    #region Helper Methods

    private static string GeneratePhoneNumber()
    {
        var random = new Random();
        return $"{random.Next(100, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}";
    }

    #endregion
}
