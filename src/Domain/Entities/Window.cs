using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Window
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // Geometry properties
        public double Width { get; set; } // in feet
        public double Height { get; set; } // in feet
        public double Area { get; set; } // in square feet (calculated or specified)

        // Frame properties
        [Required]
        [MaxLength(100)]
        public string FrameType { get; set; } = string.Empty; // e.g., "Vinyl", "Wood", "Aluminum", "Fiberglass"

        [MaxLength(500)]
        public string? FrameDetails { get; set; } // Additional frame specifications

        // Glazing properties
        [Required]
        [MaxLength(100)]
        public string GlazingType { get; set; } = string.Empty; // e.g., "Double", "Triple", "Single"

        [MaxLength(500)]
        public string? GlazingDetails { get; set; } // e.g., "Low-E coating", "Argon fill"

        // Energy modeling properties
        public double? UValue { get; set; } // Thermal transmittance (BTU/hr·ft²·°F)
        public double? SolarHeatGainCoefficient { get; set; } // SHGC (0-1)
        public double? VisibleTransmittance { get; set; } // VT (0-1)
        public double? AirLeakage { get; set; } // cfm/ft² at 75 Pa

        // Performance ratings
        [MaxLength(50)]
        public string? EnergyStarRating { get; set; } // Energy Star qualification
        [MaxLength(50)]
        public string? NFRCRating { get; set; } // NFRC certification number

        // Orientation and location
        [MaxLength(50)]
        public string? Orientation { get; set; } // North, South, East, West, etc.
        [MaxLength(100)]
        public string? Location { get; set; } // Room or building location
        [MaxLength(50)]
        public string? InstallationType { get; set; } // New construction, Replacement, etc.

        // Operational properties
        [MaxLength(100)]
        public string? OperationType { get; set; } // Fixed, Casement, Double-hung, Sliding, etc.
        public bool? HasScreens { get; set; }
        public bool? HasStormWindows { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}