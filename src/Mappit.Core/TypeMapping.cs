using System;

namespace Mappit
{
    /// <summary>
    /// Represents a mapping between two types
    /// </summary>
    public abstract class TypeMapping
    {
        /// <summary>
        /// Maps the source object to the destination type
        /// </summary>
        /// <param name="source">Source object</param>
        /// <returns>Mapped destination object</returns>
        public abstract object Map(object source);
    }

    /// <summary>
    /// Represents a mapping between two types
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TDestination">Destination type</typeparam>
    public abstract class TypeMapping<TSource, TDestination> : TypeMapping
    {
        /// <summary>
        /// Maps the source object to the destination type
        /// </summary>
        /// <param name="source">Source object</param>
        /// <returns>Mapped destination object</returns>
        public abstract TDestination Map(TSource source);

        /// <summary>
        /// Maps the source object to the destination type
        /// </summary>
        /// <param name="source">Source object</param>
        /// <returns>Mapped destination object</returns>
        public override object Map(object source)
        {
            if (source is TSource typedSource)
            {
                return Map(typedSource);
            }

            throw new InvalidOperationException($"Cannot map from {source.GetType()} to {typeof(TDestination)}");
        }
    }
}