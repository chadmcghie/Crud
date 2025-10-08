using Domain.Entities;
using Tests.Unit.Backend.TestData;

namespace Tests.Unit.Backend.Domain;

public class WindowTests
{
    public class Construction : WindowTests
    {
        [Fact]
        public void WithValidData_ShouldCreateWindow()
        {
            // Arrange & Act
            var window = WindowTestDataBuilder.Default()
                .WithName("Living Room Window")
                .WithDimensions(3.0, 4.0, 12.0)
                .WithFrameType("Vinyl")
                .WithGlazingType("Double")
                .WithThermalProperties(0.30, 0.25, 0.70)
                .Build();

            // Assert
            Assert.NotNull(window);
            Assert.NotEqual(Guid.Empty, window.Id);
            Assert.Equal("Living Room Window", window.Name);
            Assert.Equal(3.0, window.Width);
            Assert.Equal(4.0, window.Height);
            Assert.Equal(12.0, window.Area);
            Assert.Equal("Vinyl", window.FrameType);
            Assert.Equal("Double", window.GlazingType);
            Assert.Equal(0.30, window.UValue);
            Assert.Equal(0.25, window.SolarHeatGainCoefficient);
            Assert.Equal(0.70, window.VisibleTransmittance);
            Assert.True((DateTime.UtcNow - window.CreatedAt).TotalSeconds < 1);
        }

        [Fact]
        public void ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var window1 = WindowTestDataBuilder.Default().Build();
            var window2 = WindowTestDataBuilder.Default().Build();

            // Assert
            Assert.NotEqual(window2.Id, window1.Id);
        }

        [Fact]
        public void ShouldSetCreatedAtToCurrentTime()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var window = WindowTestDataBuilder.Default().Build();

            // Assert
            var afterCreation = DateTime.UtcNow;
            Assert.True(window.CreatedAt >= beforeCreation);
            Assert.True(window.CreatedAt <= afterCreation);
        }
    }

    public class EnergyProperties : WindowTests
    {
        [Fact]
        public void WithEnergyProperties_ShouldSetCorrectly()
        {
            // Arrange & Act
            var window = WindowTestDataBuilder.Default()
                .WithThermalProperties(0.25, 0.30, 0.75)
                .Build();

            // Assert
            Assert.Equal(0.25, window.UValue);
            Assert.Equal(0.30, window.SolarHeatGainCoefficient);
            Assert.Equal(0.75, window.VisibleTransmittance);
        }

        [Fact]
        public void WithOptionalEnergyProperties_ShouldSetCorrectly()
        {
            // Arrange & Act
            var window = WindowTestDataBuilder.Default().Build();

            // Assert
            Assert.Equal(0.1, window.AirLeakage);
            Assert.Equal("Yes", window.EnergyStarRating);
            Assert.Equal("NFRC-12345", window.NFRCRating);
        }
    }

    public class OperationalProperties : WindowTests
    {
        [Fact]
        public void WithOperationalProperties_ShouldSetCorrectly()
        {
            // Arrange & Act
            var window = WindowTestDataBuilder.Default().Build();

            // Assert
            Assert.Equal("Double-hung", window.OperationType);
            Assert.True(window.HasScreens);
            Assert.False(window.HasStormWindows);
        }
    }
}

