using System;
using Xunit;

namespace Mappit.Tests
{
    public class MissingPropertyTests
    {
        [Fact]
        public void ClassLevelIgnoreMissingProperties_ShouldApplyToAllMappings()
        {
            // Arrange
            var source = new SourceWithExtraProps
            {
                Id = 1,
                Name = "Test",
                ExtraProperty1 = "Extra1",
                ExtraProperty2 = 42
            };
            
            var mapper = new ClassLevelIgnoreMapper();
            
            // Act
            var target = mapper.Map(source);
            
            // Assert - Only matching properties should be mapped
            Assert.Equal(source.Id, target.Id);
            Assert.Equal(source.Name, target.Name);
        }
        
        [Fact]
        public void MethodLevelOverride_ShouldTakePrecedence()
        {
            // Arrange
            var source = new SourceWithExtraProps
            {
                Id = 1,
                Name = "Test",
                ExtraProperty1 = "Extra1",
                ExtraProperty2 = 42
            };

            // This uses the mapping which has [IgnoreMissingPropertiesOnTarget]
            var mapper = new MethodLevelOverrideMapper();
            
            // Act
            var target = mapper.Map(source);
            
            // Assert - Only matching properties should be mapped
            Assert.Equal(source.Id, target.Id);
            Assert.Equal(source.Name, target.Name);
        }
    }

    // Test models
    public class SourceWithExtraProps
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string ExtraProperty1 { get; set; }
        public int ExtraProperty2 { get; set; }
    }

    public class TargetWithFewerProps
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }

    // Class-level ignore setting
    [Mappit(IgnoreMissingPropertiesOnTarget = true)]
    public partial class ClassLevelIgnoreMapper
    {
        // This inherits the class-level setting
        public partial TargetWithFewerProps Map(SourceWithExtraProps source);
    }

    // Class with mix of settings to test override behavior
    [Mappit(IgnoreMissingPropertiesOnTarget = false)]
    public partial class MethodLevelOverrideMapper
    {
        // Override class-level setting
        [IgnoreMissingPropertiesOnTarget]
        public partial TargetWithFewerProps Map(SourceWithExtraProps source);
        
        // This would fail to compile if uncommented:
        // public partial TargetWithFewerProps MapSourceToTargetNoIgnore(SourceWithExtraProps source);
    }
}
