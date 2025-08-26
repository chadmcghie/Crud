using Ardalis.GuardClauses;

namespace Domain.Entities
{
    public class Window
    {
        private string _name = string.Empty;
        private string? _description;
        private string _frameType = string.Empty;
        private string? _frameDetails;
        private string _glazingType = string.Empty;
        private string? _glazingDetails;
        private string? _energyStarRating;
        private string? _nfrcRating;
        private string? _orientation;
        private string? _location;
        private string? _installationType;
        private string? _operationType;

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name 
        { 
            get => _name;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(value));
                Guard.Against.StringTooLong(value, 200, nameof(value));
                _name = value;
            }
        }

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
        public string FrameType 
        { 
            get => _frameType;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(value));
                Guard.Against.StringTooLong(value, 100, nameof(value));
                _frameType = value;
            }
        }

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
        public string GlazingType 
        { 
            get => _glazingType;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(value));
                Guard.Against.StringTooLong(value, 100, nameof(value));
                _glazingType = value;
            }
        }

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

        public Window(string name, string frameType, string glazingType, string? description = null)
        {
            Name = name; // This will use the setter validation
            FrameType = frameType; // This will use the setter validation
            GlazingType = glazingType; // This will use the setter validation
            Description = description; // This will use the setter validation
        }

        // Parameterless constructor for EF Core and other frameworks
        public Window()
        {
        }
    }
}