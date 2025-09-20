using Ardalis.GuardClauses;
using Domain.Exceptions;

namespace Domain.Entities
{
    public class Window : BaseEntity
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            private set
            {
                _name = Guard.Against.NullOrEmpty(value, nameof(value));
                Guard.Against.StringTooLong(value, 200, nameof(value));
            }
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 1000, nameof(value));
                }
                _description = value;
            }
        }

        // Geometry properties
        private double _width;
        public double Width
        {
            get => _width;
            private set
            {
                if (value <= 0)
                    throw new DomainException("Window width must be greater than zero");
                _width = value;
            }
        }

        private double _height;
        public double Height
        {
            get => _height;
            private set
            {
                if (value <= 0)
                    throw new DomainException("Window height must be greater than zero");
                _height = value;
            }
        }

        // Area is calculated from width and height
        public double Area => Width * Height;

        // Frame properties
        private string _frameType = string.Empty;
        public string FrameType
        {
            get => _frameType;
            private set
            {
                _frameType = Guard.Against.NullOrEmpty(value, nameof(value));
                Guard.Against.StringTooLong(value, 100, nameof(value));
            }
        }

        private string? _frameDetails;
        public string? FrameDetails
        {
            get => _frameDetails;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 500, nameof(value));
                }
                _frameDetails = value;
            }
        }

        // Glazing properties
        private string _glazingType = string.Empty;
        public string GlazingType
        {
            get => _glazingType;
            private set
            {
                _glazingType = Guard.Against.NullOrEmpty(value, nameof(value));
                Guard.Against.StringTooLong(value, 100, nameof(value));
            }
        }

        private string? _glazingDetails;
        public string? GlazingDetails
        {
            get => _glazingDetails;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 500, nameof(value));
                }
                _glazingDetails = value;
            }
        }

        // Energy modeling properties
        private double? _uValue;
        public double? UValue
        {
            get => _uValue;
            private set
            {
                if (value.HasValue && value.Value < 0)
                    throw new DomainException("U-Value cannot be negative");
                _uValue = value;
            }
        }

        private double? _solarHeatGainCoefficient;
        public double? SolarHeatGainCoefficient
        {
            get => _solarHeatGainCoefficient;
            private set
            {
                if (value.HasValue && (value.Value < 0 || value.Value > 1))
                    throw new DomainException("Solar Heat Gain Coefficient must be between 0 and 1");
                _solarHeatGainCoefficient = value;
            }
        }

        private double? _visibleTransmittance;
        public double? VisibleTransmittance
        {
            get => _visibleTransmittance;
            private set
            {
                if (value.HasValue && (value.Value < 0 || value.Value > 1))
                    throw new DomainException("Visible Transmittance must be between 0 and 1");
                _visibleTransmittance = value;
            }
        }

        private double? _airLeakage;
        public double? AirLeakage
        {
            get => _airLeakage;
            private set
            {
                if (value.HasValue && value.Value < 0)
                    throw new DomainException("Air Leakage cannot be negative");
                _airLeakage = value;
            }
        }

        // Performance ratings
        private string? _energyStarRating;
        public string? EnergyStarRating
        {
            get => _energyStarRating;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 50, nameof(value));
                }
                _energyStarRating = value;
            }
        }

        private string? _nfrcRating;
        public string? NFRCRating
        {
            get => _nfrcRating;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 50, nameof(value));
                }
                _nfrcRating = value;
            }
        }

        // Orientation and location
        private string? _orientation;
        public string? Orientation
        {
            get => _orientation;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 50, nameof(value));
                }
                _orientation = value;
            }
        }

        private string? _location;
        public string? Location
        {
            get => _location;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 100, nameof(value));
                }
                _location = value;
            }
        }

        private string? _installationType;
        public string? InstallationType
        {
            get => _installationType;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 50, nameof(value));
                }
                _installationType = value;
            }
        }

        // Operational properties
        private string? _operationType;
        public string? OperationType
        {
            get => _operationType;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 100, nameof(value));
                }
                _operationType = value;
            }
        }

        public bool? HasScreens { get; private set; }
        public bool? HasStormWindows { get; private set; }

        // EF Core constructor
        private Window()
        {
        }

        // Factory method for creating new Window
        public static Window Create(
            string name,
            double width,
            double height,
            string frameType,
            string glazingType,
            string? description = null)
        {
            return new Window
            {
                Name = name,
                Width = width,
                Height = height,
                FrameType = frameType,
                GlazingType = glazingType,
                Description = description
            };
        }

        // Domain methods for state changes
        public void UpdateName(string name)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));
            
            if (Name != name)
            {
                Name = name;
                MarkAsUpdated();
            }
        }

        public void UpdateDescription(string? description)
        {
            if (Description != description)
            {
                Description = description;
                MarkAsUpdated();
            }
        }

        public void UpdateDimensions(double width, double height)
        {
            var changed = false;

            if (Width != width)
            {
                Width = width;
                changed = true;
            }

            if (Height != height)
            {
                Height = height;
                changed = true;
            }

            if (changed)
            {
                MarkAsUpdated();
            }
        }

        public void UpdateFrameProperties(string frameType, string? frameDetails)
        {
            var changed = false;

            if (FrameType != frameType)
            {
                FrameType = frameType;
                changed = true;
            }

            if (FrameDetails != frameDetails)
            {
                FrameDetails = frameDetails;
                changed = true;
            }

            if (changed)
            {
                MarkAsUpdated();
            }
        }

        public void UpdateGlazingProperties(string glazingType, string? glazingDetails)
        {
            var changed = false;

            if (GlazingType != glazingType)
            {
                GlazingType = glazingType;
                changed = true;
            }

            if (GlazingDetails != glazingDetails)
            {
                GlazingDetails = glazingDetails;
                changed = true;
            }

            if (changed)
            {
                MarkAsUpdated();
            }
        }

        public void UpdateEnergyProperties(
            double? uValue,
            double? solarHeatGainCoefficient,
            double? visibleTransmittance,
            double? airLeakage)
        {
            var changed = false;

            if (UValue != uValue)
            {
                UValue = uValue;
                changed = true;
            }

            if (SolarHeatGainCoefficient != solarHeatGainCoefficient)
            {
                SolarHeatGainCoefficient = solarHeatGainCoefficient;
                changed = true;
            }

            if (VisibleTransmittance != visibleTransmittance)
            {
                VisibleTransmittance = visibleTransmittance;
                changed = true;
            }

            if (AirLeakage != airLeakage)
            {
                AirLeakage = airLeakage;
                changed = true;
            }

            if (changed)
            {
                MarkAsUpdated();
            }
        }

        public void UpdatePerformanceRatings(string? energyStarRating, string? nfrcRating)
        {
            var changed = false;

            if (EnergyStarRating != energyStarRating)
            {
                EnergyStarRating = energyStarRating;
                changed = true;
            }

            if (NFRCRating != nfrcRating)
            {
                NFRCRating = nfrcRating;
                changed = true;
            }

            if (changed)
            {
                MarkAsUpdated();
            }
        }

        public void UpdateLocationAndOrientation(
            string? location,
            string? orientation,
            string? installationType)
        {
            var changed = false;

            if (Location != location)
            {
                Location = location;
                changed = true;
            }

            if (Orientation != orientation)
            {
                Orientation = orientation;
                changed = true;
            }

            if (InstallationType != installationType)
            {
                InstallationType = installationType;
                changed = true;
            }

            if (changed)
            {
                MarkAsUpdated();
            }
        }

        public void UpdateOperationalProperties(
            string? operationType,
            bool? hasScreens,
            bool? hasStormWindows)
        {
            var changed = false;

            if (OperationType != operationType)
            {
                OperationType = operationType;
                changed = true;
            }

            if (HasScreens != hasScreens)
            {
                HasScreens = hasScreens;
                changed = true;
            }

            if (HasStormWindows != hasStormWindows)
            {
                HasStormWindows = hasStormWindows;
                changed = true;
            }

            if (changed)
            {
                MarkAsUpdated();
            }
        }
    }
}
