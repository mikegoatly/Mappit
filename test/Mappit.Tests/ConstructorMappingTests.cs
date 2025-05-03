using System;
using Xunit;

namespace Mappit.Tests
{
    public class ConstructorMappingTests
    {
        private readonly ConstructorTestMapper _mapper;

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
            var result = _mapper.Map<TargetWithConstructor>(source);

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
            var result = _mapper.Map<TargetWithMixedInitialization>(source);

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
            var result = _mapper.Map<TargetWithConstructor>(source);

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
            var result = _mapper.Map<TargetWithRequiredConstructor>(source);

            // Assert
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.Name, result.Name);
            Assert.Null(result.Description); // Should be null as not provided by source
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
            var result = _mapper.Map<TargetWithConstructor>(source);

            // Assert
            Assert.Equal(source.Identifier, result.Id);
            Assert.Equal(source.Title, result.Name);
            Assert.Equal(source.Text, result.Description);
        }
    }

    // Test classes for mapping

    public class SourceWithProperties
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
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

    public class TargetWithConstructor
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; }

        // Parameterized constructor with no default constructor
        public TargetWithConstructor(int id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }

    public class TargetWithMixedInitialization
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        // Constructor sets some properties, others are set via property setters
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
        public string Description { get; }

        // Constructor with a mix of required and optional parameters
        public TargetWithRequiredConstructor(int id, string name, string description = null)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }

    // Test mapper class
    public partial class ConstructorTestMapper : MapperBase
    {
        [MapType(typeof(SourceWithProperties), typeof(TargetWithConstructor))]
        private readonly TypeMapping _sourceToTargetMapping;

        [MapType(typeof(SourceWithProperties), typeof(TargetWithMixedInitialization))]
        private readonly TypeMapping _sourceToMixedMapping;

        [MapType(typeof(SourceWithDifferentCasing), typeof(TargetWithConstructor))]
        private readonly TypeMapping _casingTestMapping;

        [MapType(typeof(SourceWithLimitedProperties), typeof(TargetWithRequiredConstructor))]
        private readonly TypeMapping _limitedPropsMapping;

        [MapType(typeof(SourceWithCustomProperties), typeof(TargetWithConstructor))]
        [MapProperty("Identifier", "Id")]
        [MapProperty("Title", "Name")]
        [MapProperty("Text", "Description")]
        private readonly TypeMapping _customPropsMapping;
    }
}