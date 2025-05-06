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
            var target = mapper.Map<TargetWithFewerProps>(source);
            
            // Assert - Only matching properties should be mapped
            Assert.Equal(source.Id, target.Id);
            Assert.Equal(source.Name, target.Name);
        }
        
        [Fact]
        public void FieldLevelOverride_ShouldTakePrecedence()
        {
            // Arrange
            var source = new SourceWithExtraProps
            {
                Id = 1,
                Name = "Test",
                ExtraProperty1 = "Extra1",
                ExtraProperty2 = 42
            };
            
            var mapper = new FieldLevelOverrideMapper();
            
            // Act - This uses the "allowExtraProperties" mapping which has [IgnoreMissingProperties]
            var target = mapper.Map<SourceWithExtraProps, TargetWithFewerProps>(source);
            
            // Assert - Only matching properties should be mapped
            Assert.Equal(source.Id, target.Id);
            Assert.Equal(source.Name, target.Name);
        }
    }

    // Test models
    public class SourceWithExtraProps
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ExtraProperty1 { get; set; }
        public int ExtraProperty2 { get; set; }
    }

    public class TargetWithFewerProps
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // This would cause compile errors if uncommented:
    //[Mappit]
    //public partial class DefaultIgnoreMissingMapper
    //{
    //    // By default, fields ignore missing properties
    //    private TypeMapping<SourceWithExtraProps, TargetWithFewerProps> defaultFieldBehavior;
    //}

    // Class-level ignore setting
    [Mappit(IgnoreMissingPropertiesOnTarget = true)]
    public partial class ClassLevelIgnoreMapper
    {
        // This inherits the class-level setting
        private TypeMapping<SourceWithExtraProps, TargetWithFewerProps> inheritClassSetting;
    }

    // Class with mix of settings to test override behavior
    [Mappit(IgnoreMissingPropertiesOnTarget = false)]
    public partial class FieldLevelOverrideMapper
    {
        // Override class-level setting
        [IgnoreMissingPropertiesOnTarget]
        private TypeMapping<SourceWithExtraProps, TargetWithFewerProps> allowExtraProperties;
        
        // This would fail to compile if uncommented:
        // private TypeMapping<SourceWithExtraProps, TargetWithFewerProps> useClassLevelSetting;
    }
}
