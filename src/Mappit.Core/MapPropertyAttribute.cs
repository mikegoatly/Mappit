using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to define custom mapping between properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class MapPropertyAttribute : Attribute
    {
        /// <summary>
        /// Source property name
        /// </summary>
        public string SourcePropertyName { get; }
        
        /// <summary>
        /// Target property name
        /// </summary>
        public string TargetPropertyName { get; }

        /// <summary>
        /// Creates a new instance of MapPropertyAttribute
        /// </summary>
        /// <param name="sourcePropertyName">Source property name</param>
        /// <param name="targetPropertyName">Target property name</param>
        public MapPropertyAttribute(string sourcePropertyName, string targetPropertyName)
        {
            SourcePropertyName = sourcePropertyName ?? throw new ArgumentNullException(nameof(sourcePropertyName));
            TargetPropertyName = targetPropertyName ?? throw new ArgumentNullException(nameof(targetPropertyName));
        }
    }
}