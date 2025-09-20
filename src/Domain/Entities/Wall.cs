using Ardalis.GuardClauses;
using Domain.Exceptions;

namespace Domain.Entities
{
    public class Wall : BaseEntity
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
        private double _length;
        public double Length
        {
            get => _length;
            private set
            {
                if (value <= 0)
                    throw new DomainException("Wall length must be greater than zero");
                _length = value;
            }
        }

        private double _height;
        public double Height
        {
            get => _height;
            private set
            {
                if (value <= 0)
                    throw new DomainException("Wall height must be greater than zero");
                _height = value;
            }
        }

        private double _thickness;
        public double Thickness
        {
            get => _thickness;
            private set
            {
                if (value <= 0)
                    throw new DomainException("Wall thickness must be greater than zero");
                _thickness = value;
            }
        }

        // Assembly type properties
        private string _assemblyType = string.Empty;
        public string AssemblyType
        {
            get => _assemblyType;
            private set
            {
                _assemblyType = Guard.Against.NullOrEmpty(value, nameof(value));
                Guard.Against.StringTooLong(value, 500, nameof(value));
            }
        }

        private string? _assemblyDetails;
        public string? AssemblyDetails
        {
            get => _assemblyDetails;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 1000, nameof(value));
                }
                _assemblyDetails = value;
            }
        }

        // Energy modeling properties
        private double? _rValue;
        public double? RValue
        {
            get => _rValue;
            private set
            {
                if (value.HasValue && value.Value < 0)
                    throw new DomainException("R-Value cannot be negative");
                _rValue = value;
            }
        }

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

        private string? _materialLayers;
        public string? MaterialLayers
        {
            get => _materialLayers;
            private set
            {
                if (value != null)
                {
                    Guard.Against.StringTooLong(value, 2000, nameof(value));
                }
                _materialLayers = value;
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

        // EF Core constructor
        private Wall()
        {
        }

        // Factory method for creating new Wall
        public static Wall Create(
            string name,
            double length,
            double height,
            double thickness,
            string assemblyType,
            string? description = null)
        {
            return new Wall
            {
                Name = name,
                Length = length,
                Height = height,
                Thickness = thickness,
                AssemblyType = assemblyType,
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

        public void UpdateDimensions(double length, double height, double thickness)
        {
            var changed = false;

            if (Length != length)
            {
                Length = length;
                changed = true;
            }

            if (Height != height)
            {
                Height = height;
                changed = true;
            }

            if (Thickness != thickness)
            {
                Thickness = thickness;
                changed = true;
            }

            if (changed)
            {
                MarkAsUpdated();
            }
        }

        public void UpdateAssemblyType(string assemblyType)
        {
            Guard.Against.NullOrEmpty(assemblyType, nameof(assemblyType));
            
            if (AssemblyType != assemblyType)
            {
                AssemblyType = assemblyType;
                MarkAsUpdated();
            }
        }

        public void UpdateAssemblyDetails(string? assemblyDetails)
        {
            if (AssemblyDetails != assemblyDetails)
            {
                AssemblyDetails = assemblyDetails;
                MarkAsUpdated();
            }
        }

        public void UpdateEnergyProperties(double? rValue, double? uValue)
        {
            var changed = false;

            if (RValue != rValue)
            {
                RValue = rValue;
                changed = true;
            }

            if (UValue != uValue)
            {
                UValue = uValue;
                changed = true;
            }

            if (changed)
            {
                MarkAsUpdated();
            }
        }

        public void UpdateMaterialLayers(string? materialLayers)
        {
            if (MaterialLayers != materialLayers)
            {
                MaterialLayers = materialLayers;
                MarkAsUpdated();
            }
        }

        public void UpdateLocationAndOrientation(string? location, string? orientation)
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

            if (changed)
            {
                MarkAsUpdated();
            }
        }

        // Calculated property for wall area
        public double Area => Length * Height;
    }
}
