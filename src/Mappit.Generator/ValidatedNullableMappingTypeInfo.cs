using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal sealed record ValidatedNullableMappingTypeInfo : ValidatedMappingInfo
    {
        private ValidatedNullableMappingTypeInfo(
            string name,
            ITypeSymbol sourceType,
            ITypeSymbol targetType,
            SyntaxNode associatedSyntaxNode,
            bool isImplicitMapping)
            : base(name, sourceType, targetType, associatedSyntaxNode, isImplicitMapping)
        {
            SourceNullableUnderlyingType = sourceType.IsNullableType() ? sourceType.GetNullableUnderlyingType() : null;
            TargetNullableUnderlyingType = targetType.IsNullableType() ? targetType.GetNullableUnderlyingType() : null;
        }

        public ITypeSymbol? SourceNullableUnderlyingType { get; }
        public ITypeSymbol? TargetNullableUnderlyingType { get; }

        internal static ValidatedNullableMappingTypeInfo Explicit(MappingTypeInfo mapping)
        {
            return new(
                mapping.MethodName,
                mapping.SourceType,
                mapping.TargetType,
                mapping.MethodDeclaration,
                isImplicitMapping: false);
        }

        internal static ValidatedNullableMappingTypeInfo Implicit(
            ITypeSymbol sourceType,
            ITypeSymbol targetType,
            SyntaxNode originatingSyntaxNode)
        {
            return new(
                BuildImplicitMappingMethodName(sourceType, targetType),
                sourceType,
                targetType,
                originatingSyntaxNode,
                isImplicitMapping: true);
        }
    }
}