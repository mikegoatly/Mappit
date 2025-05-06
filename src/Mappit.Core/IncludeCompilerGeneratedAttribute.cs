using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to specify whether compiler-generated properties should be included in this mapping.
    /// This overrides the setting specified at the mapper class level via <see cref="MappitAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class IncludeCompilerGeneratedAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of IncludeCompilerGeneratedAttribute
        /// </summary>
        /// <param name="includeCompilerGenerated">Whether to include compiler-generated properties in this mapping</param>
        public IncludeCompilerGeneratedAttribute(bool includeCompilerGenerated)
        {
            IncludeCompilerGenerated = includeCompilerGenerated;
        }

        /// <summary>
        /// Gets whether compiler-generated properties should be included in this mapping
        /// </summary>
        public bool IncludeCompilerGenerated { get; }
    }
}
