        /// <summary>
        /// This method can be overridden to register custom <see cref=\"TypeMapping\"/> mappings.
        /// </summary>
        partial void InitializeCustomMappings();

        /// <summary>
        /// Registers a mapping between source and target types
        /// </summary>
        protected void RegisterMapping(global::System.Type sourceType, global::System.Type targetType, global::Mappit.TypeMapping mapping)
        {
            if (!_mappings.TryGetValue(sourceType, out var targetMappings))
            {
                targetMappings = new global::System.Collections.Generic.Dictionary<System.Type, Mappit.TypeMapping>();
                _mappings[sourceType] = targetMappings;
            }

            targetMappings[targetType] = mapping;
        }

        /// <summary>
        /// Registers a mapping between source and target types
        /// </summary>
        protected void RegisterMapping<TSource, TTarget>(global::Mappit.TypeMapping mapping)
        {
            RegisterMapping(typeof(TSource), typeof(TTarget), mapping);
        }

        /// <summary>
        /// Registers a mapping between source and target types
        /// </summary>
        protected void RegisterMapping<TSource, TTarget>(global::Mappit.TypeMapping<TSource, TTarget> mapping)
        {
            RegisterMapping(typeof(TSource), typeof(TTarget), mapping);
        }

        /// <summary>
        /// Maps an object of type TSource to type TTarget
        /// </summary>
        public TTarget Map<TTarget>(object source)
        {
            if (source == null)
            {
                return default;
            }

            var sourceType = source.GetType();
            var targetType = typeof(TTarget);

            return (TTarget)MapInternal(source, sourceType, targetType);
        }

        /// <summary>
        /// Maps an object of type TSource to type TTarget
        /// </summary>
        public TTarget Map<TSource, TTarget>(TSource source)
        {
            if (source == null)
            {
                return default;
            }

            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            return (TTarget)MapInternal(source, sourceType, targetType);
        }

        private object MapInternal(object source, global::System.Type sourceType, global::System.Type targetType)
        {
            // Check if we have a mapping for this source and target type
            if (_mappings.TryGetValue(sourceType, out var targetMappings) &&
                targetMappings.TryGetValue(targetType, out var mapping))
            {
                return mapping.Map(this, source);
            }

            throw new global::System.InvalidOperationException(
                $"No mapping defined from {sourceType.Name} to {targetType.Name}");
        }