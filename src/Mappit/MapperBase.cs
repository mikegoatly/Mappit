using System;
using System.Collections.Generic;

namespace Mappit
{
    /// <summary>
    /// Base class for mappers that implements the mapping logic
    /// </summary>
    public abstract class MapperBase : IMapper
    {
        private readonly Dictionary<Type, Dictionary<Type, TypeMapping>> _mappings;

        /// <summary>
        /// Creates a new instance of MappingBase
        /// </summary>
        protected MapperBase()
        {
            this._mappings = new Dictionary<Type, Dictionary<Type, TypeMapping>>();
            this.InitializeCustomMappings();
        }

        /// <summary>
        /// This method can be overridden to register custom <see cref="TypeMapping"/> mappings.
        /// </summary>
        protected virtual void InitializeCustomMappings()
        {
        }

        /// <summary>
        /// Registers a mapping between source and destination types
        /// </summary>
        protected void RegisterMapping(Type sourceType, Type destinationType, TypeMapping mapping)
        {
            if (!this._mappings.TryGetValue(sourceType, out var destMappings))
            {
                destMappings = new Dictionary<Type, TypeMapping>();
                this._mappings[sourceType] = destMappings;
            }

            destMappings[destinationType] = mapping;
        }

        /// <summary>
        /// Registers a mapping between source and destination types
        /// </summary>
        protected void RegisterMapping<TSource, TDestination>(TypeMapping mapping)
        {
            RegisterMapping(typeof(TSource), typeof(TDestination), mapping);
        }

        /// <summary>
        /// Registers a mapping between source and destination types
        /// </summary>
        protected void RegisterMapping<TSource, TDestination>(TypeMapping<TSource, TDestination> mapping)
        {
            RegisterMapping(typeof(TSource), typeof(TDestination), mapping);
        }

        /// <summary>
        /// Maps an object of type TSource to type TDestination
        /// </summary>
        public TDestination Map<TDestination>(object source)
        {
            if (source == null)
            {
                return default;
            }

            var sourceType = source.GetType();
            var destinationType = typeof(TDestination);

            return (TDestination)MapInternal(source, sourceType, destinationType);
        }

        /// <summary>
        /// Maps an object of type TSource to type TDestination
        /// </summary>
        public TDestination Map<TSource, TDestination>(TSource source)
        {
            if (source == null)
            {
                return default;
            }

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            return (TDestination)MapInternal(source, sourceType, destinationType);
        }

        private object MapInternal(object source, Type sourceType, Type destinationType)
        {
            // Check if we have a mapping for this source and destination type
            if (this._mappings.TryGetValue(sourceType, out var destMappings) &&
                destMappings.TryGetValue(destinationType, out var mapping))
            {
                return mapping.Map(this, source);
            }

            throw new InvalidOperationException(
                $"No mapping defined from {sourceType.Name} to {destinationType.Name}");
        }
    }
}