using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal static class TypeSymbolExtensions
    {
        /// <summary>
        /// Determines if this type is a nullable type (Nullable<T>)
        /// </summary>
        public static bool IsNullableType(this ITypeSymbol type)
        {
            return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        /// <summary>
        /// If this is a nullable type, returns the underlying type.
        /// Otherwise, returns the type itself.
        /// </summary>
        public static ITypeSymbol GetNullableUnderlyingType(this ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType && namedType.IsNullableType())
            {
                return namedType.TypeArguments[0];
            }
            
            return type;
        }
        
        /// <summary>
        /// Determines if this type is a struct
        /// </summary>
        public static bool IsStruct(this ITypeSymbol type)
        {
            return type.IsValueType && type.TypeKind == TypeKind.Struct;
        }
        
        /// <summary>
        /// Determines if this type is an enum
        /// </summary>
        public static bool IsEnum(this ITypeSymbol type)
        {
            return type.TypeKind == TypeKind.Enum
                || (type.IsNullableType() && type.GetNullableUnderlyingType().TypeKind == TypeKind.Enum);
        }
    }
}