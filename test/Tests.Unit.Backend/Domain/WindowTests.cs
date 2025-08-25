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
            window.Should().NotBeNull();
            window.Id.Should().NotBeEmpty();
            window.Name.Should().Be("Living Room Window");
            window.Width.Should().Be(3.0);
            window.Height.Should().Be(4.0);
            window.Area.Should().Be(12.0);
            window.FrameType.Should().Be("Vinyl");
            window.GlazingType.Should().Be("Double");
            window.UValue.Should().Be(0.30);
            window.SolarHeatGainCoefficient.Should().Be(0.25);
            window.VisibleTransmittance.Should().Be(0.70);
            window.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void ShouldGenerateUniqueIds()
        {
            // Arrange & Act
            var window1 = WindowTestDataBuilder.Default().Build();
            var window2 = WindowTestDataBuilder.Default().Build();

            // Assert
            window1.Id.Should().NotBe(window2.Id);
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
            window.CreatedAt.Should().BeOnOrAfter(beforeCreation);
            window.CreatedAt.Should().BeOnOrBefore(afterCreation);
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
            window.UValue.Should().Be(0.25);
            window.SolarHeatGainCoefficient.Should().Be(0.30);
            window.VisibleTransmittance.Should().Be(0.75);
        }

        [Fact]
        public void WithOptionalEnergyProperties_ShouldSetCorrectly()
        {
            // Arrange & Act
            var window = WindowTestDataBuilder.Default().Build();

            // Assert
            window.AirLeakage.Should().Be(0.1);
            window.EnergyStarRating.Should().Be("Yes");
            window.NFRCRating.Should().Be("NFRC-12345");
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
            window.OperationType.Should().Be("Double-hung");
            window.HasScreens.Should().BeTrue();
            window.HasStormWindows.Should().BeFalse();
        }
    }
}
