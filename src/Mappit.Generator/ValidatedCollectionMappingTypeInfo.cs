using System;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal sealed record ValidatedCollectionMappingTypeInfo : ValidatedMappingInfo
    {
        private static readonly Regex invalidMethodCharacterReplacement = new(@"[^a-zA-Z0-9_匚コᐸᐳ]");

        private ValidatedCollectionMappingTypeInfo(
            ITypeSymbol sourceType,
            ITypeSymbol targetType,
            SyntaxNode associatedSyntaxNode,
            string methodName,
            CollectionKind collectionKind,
            bool isImplicitMapping,
            (ITypeSymbol sourceElementType, ITypeSymbol targetElementType) elementTypeMap,
            (ITypeSymbol sourceKeyType, ITypeSymbol targetKeyType)? keyTypeMap = null)
            : base(methodName, sourceType, targetType, associatedSyntaxNode)
        {
            CollectionKind = collectionKind;
            ElementTypeMap = elementTypeMap;
            KeyTypeMap = keyTypeMap;
            RequiresPartialMethod = !isImplicitMapping;
            IsImplicitMapping = isImplicitMapping;
        }

        //public ValidatedCollectionMappingTypeInfo(
        //    ITypeSymbol sourceType, 
        //    ITypeSymbol targetType, 
        //    SyntaxNode associatedSyntaxNode,
        //    CollectionKind collectionKind,
        //    (ITypeSymbol sourceElementType, ITypeSymbol targetElementType) elementTypeMap,
        //    (ITypeSymbol sourceKeyType, ITypeSymbol targetKeyType)? keyTypeMap = null)
        //    : base(
        //          BuildImplicitMappingMethodName(sourceType, targetType), 
        //          sourceType, 
        //          targetType, 
        //          associatedSyntaxNode)
        //{   
        //    CollectionKind = collectionKind;
        //    ElementTypeMap = elementTypeMap;
        //    KeyTypeMap = keyTypeMap;
        //    RequiresPartialMethod = false;
        //    IsImplicitMapping = true;
        //}

        private static string BuildImplicitMappingMethodName(ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            return $"__Implicit_{FormatForMethodName(sourceType)}_to_{FormatForMethodName(targetType)}";
        }

        private static string FormatForMethodName(ITypeSymbol typeSymbol)
        {
            var name = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            name = name.Replace("[", "匚");
            name = name.Replace("]", "コ");
            name = name.Replace("<", "ᐸ");
            name = name.Replace(">", "ᐳ");

            // We'll likely have non-valid C# characters in the name, so we need to strip them out
            return invalidMethodCharacterReplacement.Replace(name, "_");
        }

        internal static ValidatedCollectionMappingTypeInfo Explicit(
            MappingTypeInfo mapping,
            CollectionKind collectionKind,
            (ITypeSymbol sourceElementType, ITypeSymbol targetElementType) elementTypeMap,
            (ITypeSymbol sourceKeyType, ITypeSymbol targetKeyType)? keyTypeMap = null)
        {
            return new(
                mapping.SourceType,
                mapping.TargetType,
                mapping.MethodDeclaration,
                mapping.MethodName,
                collectionKind,
                isImplicitMapping: false,
                elementTypeMap,
                keyTypeMap);
        }

        internal static ValidatedCollectionMappingTypeInfo Implicit(
            ITypeSymbol sourceType,
            ITypeSymbol targetType,
            SyntaxNode originatingSyntaxNode,
            CollectionKind collectionKind,
            (ITypeSymbol sourceValueType, ITypeSymbol targetValueType) elementTypeMap,
            (ITypeSymbol sourceKeyType, ITypeSymbol targetKeyType)? keyTypeMap = null)
        {
            return new(
                sourceType,
                targetType,
                originatingSyntaxNode,
                BuildImplicitMappingMethodName(sourceType, targetType),
                collectionKind,
                isImplicitMapping: true,
                elementTypeMap,
                keyTypeMap);
        }

        /// <summary>
        /// Gets whether this collection mapping is implicit (i.e. inferred as part of a mapping to a type's property)
        /// or an explicit mapping defined by the user's mapping type. Implicit mappings are not added to the mapper interface.
        /// </summary>
        public bool IsImplicitMapping { get; }

        /// <summary>
        /// The kind of collection mapping to generate.
        /// </summary>
        public CollectionKind CollectionKind { get; }

        /// <summary>
        /// For collection or dictionary mappings, the type of elements in the source and target collection
        /// </summary>
        public (ITypeSymbol sourceElementType, ITypeSymbol targetElementType) ElementTypeMap { get; }

        /// <summary>
        /// For dictionary mappings, the type of keys in the source and target dictionary
        /// </summary>
        public (ITypeSymbol sourceKeyType, ITypeSymbol targetKeyType)? KeyTypeMap { get; }

    }
}