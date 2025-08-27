using Ardalis.GuardClauses;

namespace Domain.Entities
{
    public class Window
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        private string _name = string.Empty;
        public string Name 
        { 
            get => _name;
            set 
            {
                _name = Guard.Against.NullOrEmpty(value, nameof(value));
                Guard.Against.StringTooLong(value, 200, nameof(value));
            }
        }

        private string? _description;
        public string? Description 
        { 
            get => _description;
            set 
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 1000, nameof(value));
                }
                _description = value;
            }
        }

        // Geometry properties
        public double Width { get; set; } // in feet
        public double Height { get; set; } // in feet
        public double Area { get; set; } // in square feet (calculated or specified)

        // Frame properties
        private string _frameType = string.Empty;
        public string FrameType 
        { 
            get => _frameType;
            set 
            {
                _frameType = Guard.Against.NullOrEmpty(value, nameof(value));
                Guard.Against.StringTooLong(value, 100, nameof(value));
            }
        }

        private string? _frameDetails;
        public string? FrameDetails 
        { 
            get => _frameDetails;
            set 
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
            set 
            {
                _glazingType = Guard.Against.NullOrEmpty(value, nameof(value));
                Guard.Against.StringTooLong(value, 100, nameof(value));
            }
        }

        private string? _glazingDetails;
        public string? GlazingDetails 
        { 
            get => _glazingDetails;
            set 
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 500, nameof(value));
                }
                _glazingDetails = value;
            }
        }

        // Energy modeling properties
        public double? UValue { get; set; } // Thermal transmittance (BTU/hr·ft²·°F)
        public double? SolarHeatGainCoefficient { get; set; } // SHGC (0-1)
        public double? VisibleTransmittance { get; set; } // VT (0-1)
        public double? AirLeakage { get; set; } // cfm/ft² at 75 Pa

        // Performance ratings
        private string? _energyStarRating;
        public string? EnergyStarRating 
        { 
            get => _energyStarRating;
            set 
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
            set 
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
            set 
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
            set 
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
            set 
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
            set 
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 100, nameof(value));
                }
                _operationType = value;
            }
        }

        public bool? HasScreens { get; set; }
        public bool? HasStormWindows { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}