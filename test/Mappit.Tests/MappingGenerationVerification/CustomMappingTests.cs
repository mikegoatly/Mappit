using System;
using System.Globalization;

using Xunit;

namespace Mappit.Tests.MappingGenerationVerification
{
    public class CustomMappingTests
    {
        private readonly ICustomMappingTestMapper _mapper;

        public CustomMappingTests()
        {
            _mapper = new CustomMappingTestMapper();
        }

        [Fact]
        public void Map_WithCustomPropertyMapping_ShouldMapCorrectly()
        {
            // Arrange
            var source = new SourceModel
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Status = SourceStatus.Active,
                CreatedOn = new DateTime(2023, 1, 15)
            };

            // Act
            var target = _mapper.MapSourceToTarget(source);

            // Assert
            Assert.Equal(source.Id, target.Id);
            
            Assert.Equal(source.FirstName, target.FirstName);
            Assert.Equal(source.LastName, target.LastName);

            // Custom enum mapping
            Assert.Equal(TargetStatus.Enabled, target.Status);
            // Custom property mapping
            Assert.Equal(source.CreatedOn, target.DateCreated);
        }

        [Fact]
        public void Map_WithCustomEnumMapping_ShouldMapAllEnumValues()
        {
            // Arrange - Test each enum value
            var tests = new[]
            {
                new { Source = SourceStatus.Active, Expected = TargetStatus.Enabled },
                new { Source = SourceStatus.Inactive, Expected = TargetStatus.Disabled },
                new { Source = SourceStatus.Pending, Expected = TargetStatus.AwaitingConfirmation }
            };

            foreach (var test in tests)
            {
                var source = new SourceModel
                {
                    Id = 1,
                    Status = test.Source,
                    FirstName = "John",
                    LastName = "Doe",
                };

                // Act
                var target = _mapper.MapSourceToTarget(source);

                // Assert
                Assert.Equal(test.Expected, target.Status);
            }
        }

        [Fact]
        public void Map_WithUserDefinedMappingMethod_ShouldMapCorrectly()
        {
            // Arrange
            var source = new CustomMapping { Name = "Weird" };
            // Act
            var target = _mapper.MapCustom(source);
            // Assert
            Assert.Equal("drieW", target.Name);
        }

        [Fact]
        public void Map_WithNestedModelRequiringUserDefinedMapping_ShouldMapCorrectly()
        {
            // Arrange
            var source = new CustomMappingContainer(1, new CustomMapping { Name = "Weird" });
            // Act
            var target = _mapper.Map(source);
            // Assert
            Assert.Equal(source.Id, target.Id);
            Assert.Equal("drieW", target.WeirdModel.Name);
        }

        [Fact]
        public void Map_WithStaticCustomValueConversion_ShouldMapCorrectly()
        {
            var mapper = new CustomPropertyConversionMapper();

            // Arrange
            var source = new CustomMapping { Name = "Weird" };
            // Act
            var target = mapper.Map(source);
            // Assert
            Assert.Equal("drieW", target.Name);
        }

        [Fact]
        public void Map_WithInstanceCustomValueConversion_ShouldMapCorrectly()
        {
            var mapper = new CustomPropertyConversionMapper();

            // Arrange
            var source = new CustomMapping { Name = "Weird" };
            // Act
            var target = mapper.MapWithUpperCase(source);
            // Assert
            Assert.Equal("WEIRD", target.Name);
        }

        [Fact]
        public void Map_WithInstanceCustomValueConversionToPropertyInitialization_ShouldMapCorrectly()
        {
            var mapper = new CustomPropertyConversionMapper();

            // Arrange
            var source = new CustomMappingMapped("Weird");
            // Act
            var target = mapper.Map(source);
            // Assert
            Assert.Equal("WEIRD", target.Name);
        }
    }

    // Test enum types with different values but same meaning
    public enum SourceStatus
    {
        Active = 0,
        Inactive = 1,
        Pending = 2
    }

    public enum TargetStatus
    {
        Enabled = 0,
        Disabled = 1,
        AwaitingConfirmation = 2
    }

    public class SourceModel
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public SourceStatus Status { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class TargetModel
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public TargetStatus Status { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class CustomMapping
    {
        public required string Name { get; set; }
    }

    public record CustomMappingMapped(string Name);

    public record CustomMappingContainer(int Id, CustomMapping WeirdModel);

    public record CustomMappingContainerMapped(int Id, CustomMappingMapped WeirdModel);

    [Mappit]
    public partial class CustomMappingTestMapper
    {
        [ReverseMap]
        [MapProperty(nameof(SourceModel.CreatedOn), nameof(TargetModel.DateCreated))]
        public partial TargetModel MapSourceToTarget(SourceModel source);

        [ReverseMap]
        [MapEnumValue(nameof(SourceStatus.Active), nameof(TargetStatus.Enabled))]
        [MapEnumValue(nameof(SourceStatus.Inactive), nameof(TargetStatus.Disabled))]
        [MapEnumValue(nameof(SourceStatus.Pending), nameof(TargetStatus.AwaitingConfirmation))]
        public partial TargetStatus MapSourceStatus(SourceStatus source);

        public partial CustomMappingContainerMapped Map(CustomMappingContainer source);

        // Custom implementation for some bespoke weird mapping - in this case we're 
        // reversing the string on one of the properties.
        public CustomMappingMapped? MapCustom(CustomMapping? source)
        {
            if (source is null)
            {
                return default;
            }

            return new CustomMappingMapped(new string(source.Name.Reverse().ToArray()));
        }
    }

    [Mappit]
    public partial class CustomPropertyConversionMapper
    {
        [MapProperty("Name", "Name", ValueConversionMethod = nameof(ReverseText))]
        public partial CustomMappingMapped Map(CustomMapping source);

        // Verify we don't *have* to pass the target property name if it's the same as the source
        [MapProperty("Name", ValueConversionMethod = nameof(UpperCase))]
        public partial CustomMappingMapped MapWithUpperCase(CustomMapping source);

        // Use the same name as a previous map method to ensure that we are locating the correct
        // method in the class for picking up attributes
        [MapProperty("Name", ValueConversionMethod = nameof(UpperCase))]
        public partial CustomMapping Map(CustomMappingMapped source);

        private static string ReverseText(string value) => 
            string.IsNullOrEmpty(value) ? value : new string(value.Reverse().ToArray());

#pragma warning disable CA1822 // Mark members as static
        private string UpperCase(string value) => value.ToUpper(CultureInfo.CurrentCulture);
#pragma warning restore CA1822 // Mark members as static
    }
}