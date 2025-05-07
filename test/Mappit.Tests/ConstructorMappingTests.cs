using System;

using Xunit;

namespace Mappit.Tests
{
    public class ConstructorMappingTests
    {
        private readonly IConstructorTestMapper _mapper;

        public ConstructorMappingTests()
        {
            _mapper = new ConstructorTestMapper();
        }

        [Fact]
        public void Map_ToClassWithParameterizedConstructor_ShouldUseConstructorParameters()
        {
            // Arrange
            var source = new SourceWithProperties
            {
                Id = 42,
                Name = "Test Source",
                Description = "This is a test"
            };

            // Act
            var result = _mapper.MapSourceToTarget(source);

            // Assert
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.Name, result.Name);
            Assert.Equal(source.Description, result.Description);
        }

        [Fact]
        public void Map_ToClassWithMixedInitialization_ShouldMapCorrectly()
        {
            // Arrange
            var source = new SourceWithProperties
            {
                Id = 42,
                Name = "Test Source",
                Description = "This is a test",
                CreatedDate = new DateTime(2023, 5, 15),
                IsActive = true
            };

            // Act
            var result = _mapper.MapSourceToMixed(source);

            // Assert
            // These properties should be set via constructor
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.Name, result.Name);

            // These properties should be set via property setters
            Assert.Equal(source.Description, result.Description);
            Assert.Equal(source.CreatedDate, result.CreatedDate);
            Assert.Equal(source.IsActive, result.IsActive);
        }

        [Fact]
        public void Map_WithCaseInsensitiveParameterMatching_ShouldMapCorrectly()
        {
            // Arrange
            var source = new SourceWithDifferentCasing
            {
                id = 42,
                NAME = "Test Source",
                description = "This is a test"
            };

            // Act
            var result = _mapper.MapCaseSensitiveSourceToTarget(source);

            // Assert
            Assert.Equal(source.id, result.Id);
            Assert.Equal(source.NAME, result.Name);
            Assert.Equal(source.description, result.Description);
        }

        [Fact]
        public void Map_WithParameterSubset_ShouldMapAvailableParameters()
        {
            // Arrange
            var source = new SourceWithLimitedProperties
            {
                Id = 42,
                Name = "Test Source"
            };

            // Act
            var result = _mapper.MapLimitedToRequired(source);

            // Assert
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.Name, result.Name);
        }

        [Fact]
        public void Map_WithCustomPropertyMapping_ShouldUseCustomMappingForConstructor()
        {
            // Arrange
            var source = new SourceWithCustomProperties
            {
                Identifier = 42,
                Title = "Test Source",
                Text = "This is a test"
            };

            // Act
            var result = _mapper.MapCustomPropertiesToTarget(source);

            // Assert
            Assert.Equal(source.Identifier, result.Id);
            Assert.Equal(source.Title, result.Name);
            Assert.Equal(source.Text, result.Description);
        }
    }

    public class SourceWithProperties
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class TargetWithConstructor
    {
        public TargetWithConstructor(int id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class SourceWithDifferentCasing
    {
        public int id { get; set; }
        public string NAME { get; set; }
        public string description { get; set; }
    }

    public class SourceWithLimitedProperties
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SourceWithCustomProperties
    {
        public int Identifier { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
    }

    public class TargetWithMixedInitialization
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        public TargetWithMixedInitialization(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class TargetWithRequiredConstructor
    {
        public int Id { get; }
        public string Name { get; }
        
        public TargetWithRequiredConstructor(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    [Mappit]
    public partial class ConstructorTestMapper
    {
        public partial TargetWithConstructor MapSourceToTarget(SourceWithProperties source);

        public partial TargetWithMixedInitialization MapSourceToMixed(SourceWithProperties source);

        public partial TargetWithConstructor MapCaseSensitiveSourceToTarget(SourceWithDifferentCasing source);

        public partial TargetWithRequiredConstructor MapLimitedToRequired(SourceWithLimitedProperties source);

        [MapMember("Identifier", "Id")]
        [MapMember("Title", "Name")]
        [MapMember("Text", "Description")]
        public partial TargetWithConstructor MapCustomPropertiesToTarget(SourceWithCustomProperties source);
    }
}