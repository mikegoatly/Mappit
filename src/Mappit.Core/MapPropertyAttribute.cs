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
        /// <param name="targetName">Target member name</param>
        public MapPropertyAttribute(string sourceName, string targetName)
        {
            SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            TargetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
        }

        /// <summary>
        /// Source member name
        /// </summary>
        public string SourceName { get; }

        /// <summary>
        /// Target member name
        /// </summary>
        public string TargetName { get; }
    }
}