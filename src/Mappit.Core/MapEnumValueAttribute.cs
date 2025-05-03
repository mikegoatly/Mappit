using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to define custom mapping between enum values
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class MapEnumValueAttribute : Attribute
    {
        /// <summary>
        /// Source enum value name
        /// </summary>
        public string SourceValueName { get; }
        
        /// <summary>
        /// Target enum value name
        /// </summary>
        public string TargetValueName { get; }

        /// <summary>
        /// Creates a new instance of MapEnumValueAttribute
        /// </summary>
        /// <param name="sourceValueName">Source enum value name</param>
        /// <param name="targetValueName">Target enum value name</param>
        public MapEnumValueAttribute(string sourceValueName, string targetValueName)
        {
            SourceValueName = sourceValueName ?? throw new ArgumentNullException(nameof(sourceValueName));
            TargetValueName = targetValueName ?? throw new ArgumentNullException(nameof(targetValueName));
        }
    }
}