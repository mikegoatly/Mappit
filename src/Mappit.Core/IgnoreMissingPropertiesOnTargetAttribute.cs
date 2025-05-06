using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to specify whether missing properties on the target type should be ignored.
    /// This overrides the setting specified at the mapper class level via <see cref="MappitAttribute.IgnoreMissingPropertiesOnTarget"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class IgnoreMissingPropertiesOnTargetAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="IgnoreMissingPropertiesOnTargetAttribute"/>
        /// </summary>
        /// <param name="ignoreMissingPropertiesOnTarget">Whether to ignore missing properties on the target type</param>
        public IgnoreMissingPropertiesOnTargetAttribute(bool ignoreMissingPropertiesOnTarget = true)
        {
            IgnoreMissingPropertiesOnTarget = ignoreMissingPropertiesOnTarget;
        }

        /// <summary>
        /// Gets whether missing properties on the target type should be ignored
        /// </summary>
        public bool IgnoreMissingPropertiesOnTarget { get; }
    }
}
