using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to define custom value mapping between enums.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class MapEnumValueAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="MapPropertyAttribute"/>
        /// </summary>
        /// <param name="sourceName">Source member name</param>
        /// <param name="targetName">Target member name</param>
        public MapEnumValueAttribute(string sourceName, string targetName)
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