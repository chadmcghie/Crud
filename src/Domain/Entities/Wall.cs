using Ardalis.GuardClauses;

namespace Domain.Entities
{
    public class Wall
    {
        private string _name = string.Empty;
        private string? _description;
        private string _assemblyType = string.Empty;
        private string? _assemblyDetails;

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
        public double Length { get; set; } // in feet
        public double Height { get; set; } // in feet
        public double Thickness { get; set; } // in inches

        // Assembly type properties
        public string AssemblyType 
        { 
            get => _assemblyType;
            set
            {
                Guard.Against.NullOrWhiteSpace(value, nameof(value));
                Guard.Against.StringTooLong(value, 500, nameof(value));
                _assemblyType = value;
            }
        }

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

        public Wall(string name, string assemblyType, string? description = null, string? assemblyDetails = null)
        {
            Name = name; // This will use the setter validation
            AssemblyType = assemblyType; // This will use the setter validation
            Description = description; // This will use the setter validation
            AssemblyDetails = assemblyDetails; // This will use the setter validation
        }

        // Parameterless constructor for EF Core and other frameworks
        public Wall()
        {
        }
    }
}
