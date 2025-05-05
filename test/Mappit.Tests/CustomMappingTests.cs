using System;
using Xunit;

namespace Mappit.Tests
{
    public class CustomMappingTests
    {
        private readonly IMapper _mapper;

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
            var target = _mapper.Map<TargetModel>(source);

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
                    Status = test.Source
                };

                // Act
                var target = _mapper.Map<TargetModel>(source);

                // Assert
                Assert.Equal(test.Expected, target.Status);
            }
        }

        [Fact]
        public void Map_WithWeirdMapping_ShouldMapCorrectly()
        {
            // Arrange
            var source = new WeirdModel { Name = "Weird" };
            // Act
            var target = _mapper.Map<WeirdModel>(source);
            // Assert
            Assert.Equal("drieW", target.Name);
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
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public SourceStatus Status { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class TargetModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public TargetStatus Status { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class WeirdModel
    {
        public string Name { get; set; }
    }

    [Mappit]
    public partial class CustomMappingTestMapper : MapperBase
    {
        [MapMember(nameof(SourceModel.CreatedOn), nameof(TargetModel.DateCreated))]
        private TypeMapping<SourceModel, TargetModel> sourceToTarget;

        [MapMember(nameof(SourceStatus.Active), nameof(TargetStatus.Enabled))]
        [MapMember(nameof(SourceStatus.Inactive), nameof(TargetStatus.Disabled))]
        [MapMember(nameof(SourceStatus.Pending), nameof(TargetStatus.AwaitingConfirmation))]
        private TypeMapping<SourceStatus, TargetStatus> sourceStatus;

        protected override void InitializeCustomMappings()
        {
            RegisterMapping(new WeirdMapping());
        }

        private class WeirdMapping : TypeMapping<WeirdModel, WeirdModel>
        {
            public override WeirdModel Map(IMapper mapper, WeirdModel source)
            {
                return new WeirdModel { Name = new string([.. source.Name.Reverse()]) };
            }
        }
    }
}