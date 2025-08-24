using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Wall
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // Geometry properties
        public double Length { get; set; } // in feet
        public double Height { get; set; } // in feet
        public double Thickness { get; set; } // in inches

        // Assembly type properties
        [Required]
        [MaxLength(500)]
        public string AssemblyType { get; set; } = string.Empty; // e.g., "2x4 16\" on center with R13 fiberglass insulation"

        [MaxLength(1000)]
        public string? AssemblyDetails { get; set; } // Additional details about the assembly

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
