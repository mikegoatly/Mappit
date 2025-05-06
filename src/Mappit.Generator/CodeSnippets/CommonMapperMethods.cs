        /// <summary>
        /// This method can be overridden to register custom <see cref=\"TypeMapping\"/> mappings.
        /// </summary>
        partial void InitializeCustomMappings();

        /// <summary>
        /// Registers a mapping between source and destination types
        /// </summary>
        protected void RegisterMapping(System.Type sourceType, System.Type destinationType, Mappit.TypeMapping mapping)
        {
            if (!_mappings.TryGetValue(sourceType, out var destMappings))
            {
                destMappings = new System.Collections.Generic.Dictionary<System.Type, Mappit.TypeMapping>();
                _mappings[sourceType] = destMappings;
            }

            destMappings[destinationType] = mapping;
        }

        /// <summary>
        /// Registers a mapping between source and destination types
        /// </summary>
        protected void RegisterMapping<TSource, TDestination>(Mappit.TypeMapping mapping)
        {
            RegisterMapping(typeof(TSource), typeof(TDestination), mapping);
        }

        /// <summary>
        /// Registers a mapping between source and destination types
        /// </summary>
        protected void RegisterMapping<TSource, TDestination>(Mappit.TypeMapping<TSource, TDestination> mapping)
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

        private object MapInternal(object source, System.Type sourceType, System.Type destinationType)
        {
            // Check if we have a mapping for this source and destination type
            if (_mappings.TryGetValue(sourceType, out var destMappings) &&
                destMappings.TryGetValue(destinationType, out var mapping))
            {
                return mapping.Map(this, source);
            }

            throw new System.InvalidOperationException(
                $"No mapping defined from {sourceType.Name} to {destinationType.Name}");
        }