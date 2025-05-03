using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to define mapping between two types
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MapTypeAttribute : Attribute
    {
        /// <summary>
        /// Source type for the mapping
        /// </summary>
        public Type SourceType { get; }
        
        /// <summary>
        /// Destination type for the mapping
        /// </summary>
        public Type DestinationType { get; }

        /// <summary>
        /// Creates a new instance of MapTypeAttribute
        /// </summary>
        /// <param name="sourceType">Source type</param>
        /// <param name="destinationType">Destination type</param>
        public MapTypeAttribute(Type sourceType, Type destinationType)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
        }
    }
}