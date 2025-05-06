using System;

namespace Mappit
{
    /// <summary>
    /// Interface for mapping between different types
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// Maps an object to type <typeparamref name="TTarget"/>. The type of the source object is determined at runtime
        /// and must be the type expected by the mapping.
        /// </summary>
        /// <typeparam name="TTarget">The target type</typeparam>
        /// <param name="source">The source object</param>
        /// <returns>Mapped target object</returns>
        TTarget Map<TTarget>(object source);

        /// <summary>
        /// Maps an object of type <typeparamref name="TSource"/> to type <typeparamref name="TTarget"/>
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TTarget">The target type</typeparam>
        /// <param name="mapper">The mapper instance that can be used to map child types.</param>
        /// <param name="source">The source object</param>
        /// <returns>Mapped target object</returns>
        TTarget Map<TSource, TTarget>(TSource source);
    }
}