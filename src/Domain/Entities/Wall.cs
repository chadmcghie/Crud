using Ardalis.GuardClauses;

namespace Domain.Entities
{
    public class Wall
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
        public double Length { get; set; } // in feet
        public double Height { get; set; } // in feet
        public double Thickness { get; set; } // in inches

        // Assembly type properties
        private string _assemblyType = string.Empty;
        public string AssemblyType 
        { 
            get => _assemblyType;
            set 
            {
                _assemblyType = Guard.Against.NullOrEmpty(value, nameof(value));
                Guard.Against.StringTooLong(value, 500, nameof(value));
            }
        }

        private string? _assemblyDetails;
        public string? AssemblyDetails 
        { 
            get => _assemblyDetails;
            set 
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 1000, nameof(value));
                }
                _assemblyDetails = value;
            }
        }

        // Energy modeling properties
        public double? RValue { get; set; } // Thermal resistance
        public double? UValue { get; set; } // Thermal transmittance
        public string? MaterialLayers { get; set; } // Detailed breakdown of material layers

        // Orientation and location
        public string? Orientation { get; set; } // North, South, East, West, etc.
        public string? Location { get; set; } // Interior, Exterior, etc.

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
