using System;

namespace Mappit
{
    /// <summary>
    /// Interface for mapping between different types
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// Maps an object of type TSource to type TDestination
        /// </summary>
        /// <typeparam name="TDestination">The destination type</typeparam>
        /// <param name="source">The source object</param>
        /// <returns>Mapped destination object</returns>
        TDestination Map<TDestination>(object source);

        /// <summary>
        /// Maps an object of type TSource to type TDestination
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TDestination">The destination type</typeparam>
        /// <param name="mapper">The mapper instance that can be used to map child types.</param>
        /// <param name="source">The source object</param>
        /// <returns>Mapped destination object</returns>
        TDestination Map<TSource, TDestination>(TSource source);
    }
}