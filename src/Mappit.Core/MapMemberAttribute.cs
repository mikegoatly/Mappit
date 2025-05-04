using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to define custom mapping between types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class MapMemberAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of MapEnumValueAttribute
        /// </summary>
        /// <param name="sourceName">Source enum value name</param>
        /// <param name="targetName">Target enum value name</param>
        public MapMemberAttribute(string sourceName, string targetName)
        {
            SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
            TargetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
        }

        /// <summary>
        /// Source enum value name
        /// </summary>
        public string SourceName { get; }

        /// <summary>
        /// Target enum value name
        /// </summary>
        public string TargetName { get; }
    }
}