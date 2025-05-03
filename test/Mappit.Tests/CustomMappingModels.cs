using System;
using Xunit;

namespace Mappit.Tests
{
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

    // Test models with custom property names
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
        public string Name { get; set; }  // Should contain FirstName + LastName
        public TargetStatus Status { get; set; }
        public DateTime DateCreated { get; set; }  // Maps from CreatedOn
    }

    public class WeirdModel
    {
        public string Name { get; set; }
    }

    // Custom mapper with property and enum mappings
    public partial class CustomMappingTestMapper : MapperBase
    {
        [MapType(typeof(SourceModel), typeof(TargetModel))]
        [MapProperty(nameof(SourceModel.CreatedOn), nameof(TargetModel.DateCreated))]
        [MapEnumValue(nameof(SourceStatus.Active), nameof(TargetStatus.Enabled))]
        [MapEnumValue(nameof(SourceStatus.Inactive), nameof(TargetStatus.Disabled))]
        [MapEnumValue(nameof(SourceStatus.Pending), nameof(TargetStatus.AwaitingConfirmation))]
        private TypeMapping sourceToTarget;

        protected override void InitializeCustomMappings()
        {
            RegisterMapping(new WeirdMapping());
        }

        private class WeirdMapping : TypeMapping<WeirdModel, WeirdModel>
        {
            public override WeirdModel Map(WeirdModel source)
            {
                return new WeirdModel { Name = new string([..source.Name.Reverse()]) };
            }
        }
    }
}