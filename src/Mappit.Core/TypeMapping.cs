using System;

namespace Mappit
{
    /// <summary>
    /// Represents a mapping between two types
    /// </summary>
    public abstract class TypeMapping
    {
        /// <summary>
        /// Maps the source object to the target type
        /// </summary>
        /// <param name="mapper">The mapper instance that can be used to map child types.</param>
        /// <param name="source">Source object</param>
        /// <returns>Mapped target object</returns>
        public abstract object Map(IMapper mapper, object source);
    }

    /// <summary>
    /// Represents a mapping between two types
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TTarget">Target type</typeparam>
    public abstract class TypeMapping<TSource, TTarget> : TypeMapping
    {
        /// <summary>
        /// Maps the source object to the target type
        /// </summary>
        /// <param name="source">Source object</param>
        /// <returns>Mapped target object</returns>
        public abstract TTarget Map(IMapper mapper, TSource source);

        /// <summary>
        /// Maps the source object to the target type
        /// </summary>
        /// <param name="source">Source object</param>
        /// <returns>Mapped target object</returns>
        public override object Map(IMapper mapper, object source)
        {
            if (source is TSource typedSource)
            {
                return Map(mapper, typedSource);
            }

            throw new InvalidOperationException($"Cannot map from {source.GetType()} to {typeof(TTarget)}");
        }
    }
}