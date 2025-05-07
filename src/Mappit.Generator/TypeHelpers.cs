using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal static class TypeHelpers
    {
        private static readonly HashSet<string> collectionInterfaceTypes = new()
        {
            "System.Collections.Generic.IEnumerable`1",
            "System.Collections.Generic.ICollection`1",
            "System.Collections.Generic.IList`1",
            "System.Collections.Generic.IReadOnlyCollection`1",
            "System.Collections.Generic.IReadOnlyList`1",
        };

        private static readonly HashSet<string> setInterfaceTypes = new()
        {
            "System.Collections.Generic.ISet`1",
            "System.Collections.Generic.IReadOnlySet`1",
        };

        private static readonly HashSet<string> dictionaryInterfaceTypes = new()
        {
            "System.Collections.Generic.IDictionary`2",
            "System.Collections.Generic.IReadOnlyDictionary`2",
        };

        /// <summary>
        /// Determines if the type is a supported collection type.
        /// </summary>
        public static bool IsCollectionType(ITypeSymbol type, out ITypeSymbol? elementType)
        {
            elementType = null;

            // Skip if the type is a string (strings implement IEnumerable<char> but we don't want to treat them as collections)
            if (type.SpecialType == SpecialType.System_String)
            {
                return false;
            }

            // Special case for arrays
            if (type.TypeKind == TypeKind.Array)
            {
                elementType = ((IArrayTypeSymbol)type).ElementType;
                return true;
            }

            // Check to see if the type itself is one of the supported interfaces
            var name = GetSearchableInterfaceName(type);
            if (type is INamedTypeSymbol namedTypeSymbol && (collectionInterfaceTypes.Contains(name) || setInterfaceTypes.Contains(name)))
            {
                elementType = namedTypeSymbol.TypeArguments[0];
                return true;
            }

            // Check if the type implements any of these collection interfaces
            foreach (var i in type.AllInterfaces)
            {
                var interfaceName = GetSearchableInterfaceName(i);
                if (collectionInterfaceTypes.Contains(interfaceName) || setInterfaceTypes.Contains(interfaceName))
                {
                    elementType = i.TypeArguments[0];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the type is a supported dictionary type.
        /// </summary>
        public static bool IsDictionaryType(ITypeSymbol type, out ITypeSymbol? keyType, out ITypeSymbol? valueType)
        {
            keyType = null;
            valueType = null;

            // First check if the type itself is one of the supported interfaces
            if (type is INamedTypeSymbol namedTypeSymbol && dictionaryInterfaceTypes.Contains(GetSearchableInterfaceName(type)))
            {
                keyType = namedTypeSymbol.TypeArguments[0];
                valueType = namedTypeSymbol.TypeArguments[1];
                return true;
            }

            // Check if the type implements any known dictionary interfaces
            foreach (var i in type.AllInterfaces)
            {
                var interfaceName = GetSearchableInterfaceName(i);
                if (dictionaryInterfaceTypes.Contains(interfaceName))
                {
                    keyType = i.TypeArguments[0];
                    valueType = i.TypeArguments[1];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Formats the interface name in such a way that it can be searched in our lookup tables. These
        /// don't contain the actual generic type arguments, e.g. System.Collections.Generic.IEnumerable`1
        /// </summary>
        private static string GetSearchableInterfaceName(ITypeSymbol i)
        {
            return $"{i.ContainingNamespace.ToDisplayString()}.{i.MetadataName}";
        }

        /// <summary>
        /// Infers the appropriate concrete type for a collection interface.
        /// </summary>
        public static string InferConcreteCollectionType(ITypeSymbol collectionType, ITypeSymbol elementType)
        {
            // If it's an array, we don't need to infer anything
            if (collectionType.TypeKind == TypeKind.Array)
            {
                return $"{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[]";
            }

            // If it's already a concrete type and not an interface, use it
            if (collectionType.TypeKind != TypeKind.Interface)
            {
                return collectionType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            // For most interface types, we'll map to a List<T>, unless it's a set type
            string elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Check if the type is a set interface

            if (setInterfaceTypes.Contains(GetSearchableInterfaceName(collectionType)))
            {
                return $"global::System.Collections.Generic.HashSet<{elementTypeName}>";
            }


            return $"global::System.Collections.Generic.List<{elementTypeName}>";
        }

        /// <summary>
        /// Infers the appropriate concrete type for a dictionary interface.
        /// </summary>
        public static string InferConcreteDictionaryType(ITypeSymbol interfaceType, ITypeSymbol keyType, ITypeSymbol valueType)
        {
            // If it's already a concrete type and not an interface, use it
            if (interfaceType.TypeKind != TypeKind.Interface)
            {
                return interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            // For everything else, we'll just use a dictionary
            string keyTypeName = keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string valueTypeName = valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            return $"global::System.Collections.Generic.Dictionary<{keyTypeName}, {valueTypeName}>";
        }
    }
}