using Domain.Entities;

namespace Tests.Unit.Backend.TestData;

public class WindowTestDataBuilder
{
    private string _name = "Living Room Window";
    private string? _description = "Double-hung window with low-E coating";
    private double _width = 3.0;
    private double _height = 4.0;
    private double _area = 12.0;
    private string _frameType = "Vinyl";
    private string? _frameDetails = "White vinyl frame";
    private string _glazingType = "Double";
    private string? _glazingDetails = "Low-E coating with argon fill";
    private double? _uValue = 0.30;
    private double? _solarHeatGainCoefficient = 0.25;
    private double? _visibleTransmittance = 0.70;
    private double? _airLeakage = 0.1;
    private string? _energyStarRating = "Yes";
    private string? _nfrcRating = "NFRC-12345";
    private string? _orientation = "South";
    private string? _location = "Living Room";
    private string? _installationType = "New Construction";
    private string? _operationType = "Double-hung";
    private bool? _hasScreens = true;
    private bool? _hasStormWindows = false;

    public WindowTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public WindowTestDataBuilder WithDimensions(double width, double height, double area)
    {
        _width = width;
        _height = height;
        _area = area;
        return this;
    }

    public WindowTestDataBuilder WithFrameType(string frameType)
    {
        _frameType = frameType;
        return this;
    }

    public WindowTestDataBuilder WithGlazingType(string glazingType)
    {
        _glazingType = glazingType;
        return this;
    }

    public WindowTestDataBuilder WithThermalProperties(double? uValue, double? shgc, double? vt)
    {
        _uValue = uValue;
        _solarHeatGainCoefficient = shgc;
        _visibleTransmittance = vt;
        return this;
    }

    public Window Build()
    {
        var window = Window.Create(_name, _width, _height, _frameType, _glazingType, _description);

        // Set additional properties using domain methods
        if (_frameDetails != null)
            window.UpdateFrameProperties(_frameType, _frameDetails);

        if (_glazingDetails != null)
            window.UpdateGlazingProperties(_glazingType, _glazingDetails);

        window.UpdateEnergyProperties(_uValue, _solarHeatGainCoefficient, _visibleTransmittance, _airLeakage);
        window.UpdatePerformanceRatings(_energyStarRating, _nfrcRating);
        window.UpdateLocationAndOrientation(_location, _orientation, _installationType);
        window.UpdateOperationalProperties(_operationType, _hasScreens, _hasStormWindows);

        return window;
    }

    public static WindowTestDataBuilder Default() => new();
}

