using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to define custom property mapping between types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MapPropertyAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="MapPropertyAttribute"/>
        /// </summary>
        /// <param name="sourceName">Source member name</param>
        /// <param name="targetName">Target member name. If left null, Mappit will assume it is the same as source member name</param>
        public MapPropertyAttribute(string sourceName, string? targetName = null)
        {
            SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            TargetName = targetName ?? sourceName;
        }

        /// <summary>
        /// Source member name
        /// </summary>
        public string SourceName { get; }

        /// <summary>
        /// Target member name
        /// </summary>
        public string TargetName { get; }

        /// <summary>
        /// The name of the method to use for converting the source value to the target value.
        /// The method must have a return type that matches the target property type and take a single parameter of the source property type.
        /// </summary>
        public string? ValueConversionMethod { get; set; }
    }
}