using System;

namespace Mappit
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MappitAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to ignore missing properties on the target type.
        /// </summary>
        /// <remarks>
        /// Default is false, meaning the absence of a property in the target will generate an error.
        /// </remarks>
        public bool IgnoreMissingPropertiesOnTarget { get; set; } = false;
    }
}
