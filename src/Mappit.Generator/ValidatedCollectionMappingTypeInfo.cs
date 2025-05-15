using System;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal sealed record ValidatedCollectionMappingTypeInfo : ValidatedMappingInfo
    {
        private ValidatedCollectionMappingTypeInfo(
            ITypeSymbol sourceType,
            ITypeSymbol targetType,
            SyntaxNode associatedSyntaxNode,
            string methodName,
            CollectionKind collectionKind,
            bool isImplicitMapping,
            (ITypeSymbol sourceElementType, ITypeSymbol targetElementType) elementTypeMap,
            (ITypeSymbol sourceKeyType, ITypeSymbol targetKeyType)? keyTypeMap = null)
            : base(methodName, sourceType, targetType, associatedSyntaxNode, isImplicitMapping)
        {
            CollectionKind = collectionKind;
            ElementTypeMap = elementTypeMap;
            KeyTypeMap = keyTypeMap;
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