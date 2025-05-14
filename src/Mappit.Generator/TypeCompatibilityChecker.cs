using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal static class TypeCompatibilityChecker
    {
        /// <summary>
        /// Determines if two types are compatible for mapping purposes
        /// </summary>
        public static bool AreCompatibleTypes(MapperClassInfoBase mapperClass, ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            // Simple case: if the types are the same, they're compatible
            if (sourceType.Equals(targetType, SymbolEqualityComparer.Default))
            {
                return true;
            }

            // Check if the types are compatible because they've already been mapped
            if (mapperClass.HasHapping(sourceType, targetType))
            {
                return true;
            }

            // Check for nullable types
            var sourceIsNullable = sourceType.IsNullableType();
            var targetIsNullable = targetType.IsNullableType();

            // If one or the other is nullable, check if their underlying types are compatible
            if (sourceIsNullable || targetIsNullable)
            {
                var sourceUnderlyingType = sourceIsNullable ? sourceType.GetNullableUnderlyingType() : sourceType;
                var targetUnderlyingType = targetIsNullable ? targetType.GetNullableUnderlyingType() : targetType;

                // If the underlying types are compatible, then the nullable and non-nullable types are compatible
                return AreCompatibleTypes(mapperClass, sourceUnderlyingType, targetUnderlyingType);
            }

            // Dictionary types - these also have collection/enumerable interfaces, so we need to check them first
            if (TypeHelpers.IsDictionaryType(sourceType, out var sourceKeyType, out var sourceValueType) &&
                TypeHelpers.IsDictionaryType(targetType, out var targetKeyType, out var targetValueType))
            {
                if (sourceKeyType != null && targetKeyType != null && sourceValueType != null && targetValueType != null)
                {
                    // Dictionaries are compatible if their key and value types are compatible
                    return AreCompatibleTypes(mapperClass, sourceKeyType, targetKeyType) &&
                           AreCompatibleTypes(mapperClass, sourceValueType, targetValueType);
                }
            }

            // Check for collection types
            if (TypeHelpers.IsCollectionType(sourceType, out var sourceElementType) &&
                TypeHelpers.IsCollectionType(targetType, out var targetElementType))
            {
                if (sourceElementType != null && targetElementType != null)
                {
                    // Collections are compatible if their element types are compatible
                    return AreCompatibleTypes(mapperClass, sourceElementType, targetElementType);
                }
            }

            return false;
        }
    }
}