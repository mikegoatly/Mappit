using System;
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
            var source = new WeirdModel { Name = "Weird" };
            // Act
            var target = _mapper.MapWeirdModel(source);
            // Assert
            Assert.Equal("drieW", target.Name);
        }

        [Fact]
        public void Map_WithNestedModelRequiringUserDefinedMapping_ShouldMapCorrectly()
        {
            // Arrange
            var source = new WeirdModelContainer(1, new WeirdModel { Name = "Weird" });
            // Act
            var target = _mapper.Map(source);
            // Assert
            Assert.Equal(source.Id, target.Id);
            Assert.Equal("drieW", target.WeirdModel.Name);
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

    public class WeirdModel
    {
        public required string Name { get; set; }
    }

    public record WeirdModelMapped(string Name);

    public record WeirdModelContainer(int Id, WeirdModel WeirdModel);

    public record WeirdModelContainerMapped(int Id, WeirdModelMapped WeirdModel);

    [Mappit]
    public partial class CustomMappingTestMapper
    {
        [ReverseMap]
        [MapMember(nameof(SourceModel.CreatedOn), nameof(TargetModel.DateCreated))]
        public partial TargetModel MapSourceToTarget(SourceModel source);

        [ReverseMap]
        [MapMember(nameof(SourceStatus.Active), nameof(TargetStatus.Enabled))]
        [MapMember(nameof(SourceStatus.Inactive), nameof(TargetStatus.Disabled))]
        [MapMember(nameof(SourceStatus.Pending), nameof(TargetStatus.AwaitingConfirmation))]
        public partial TargetStatus MapSourceStatus(SourceStatus source);

        public partial WeirdModelContainerMapped Map(WeirdModelContainer source);

        // Custom implementation for some bespoke weird mapping - in this case we're 
        // reversing the string on one of the properties.
        public WeirdModelMapped? MapWeirdModel(WeirdModel? source)
        {
            if (source is null)
            {
                return default;
            }

            return new WeirdModelMapped(new string(source.Name.Reverse().ToArray()));
        }
    }
}