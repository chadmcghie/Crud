using Domain.Entities;

namespace Tests.Unit.Backend.TestData;

public class WallTestDataBuilder
{
    private string _name = "Exterior Wall";
    private string? _description = "Standard exterior wall assembly";
    private double _length = 10.0;
    private double _height = 9.0;
    private double _thickness = 6.0;
    private string _assemblyType = "2x4 16\" on center with R13 fiberglass insulation";
    private string? _assemblyDetails = "Standard wood frame construction";
    private double? _rValue = 13.0;
    private double? _uValue = 0.077;
    private string? _materialLayers = "Drywall, Insulation, Sheathing, Siding";
    private string? _orientation = "North";
    private string? _location = "Exterior";

    public WallTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public WallTestDataBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public WallTestDataBuilder WithDimensions(double length, double height, double thickness)
    {
        _length = length;
        _height = height;
        _thickness = thickness;
        return this;
    }

    public WallTestDataBuilder WithAssemblyType(string assemblyType)
    {
        _assemblyType = assemblyType;
        return this;
    }

    public WallTestDataBuilder WithThermalProperties(double? rValue, double? uValue)
    {
        _rValue = rValue;
        _uValue = uValue;
        return this;
    }

    public Wall Build()
    {
        return new Wall
        {
            Name = _name,
            Description = _description,
            Length = _length,
            Height = _height,
            Thickness = _thickness,
            AssemblyType = _assemblyType,
            AssemblyDetails = _assemblyDetails,
            RValue = _rValue,
            UValue = _uValue,
            MaterialLayers = _materialLayers,
            Orientation = _orientation,
            Location = _location
        };
    }

    public static WallTestDataBuilder Default() => new();
}
