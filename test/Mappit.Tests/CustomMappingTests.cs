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
            // Name property wasn't mapped, should be default
            Assert.Null(target.Name);
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
}