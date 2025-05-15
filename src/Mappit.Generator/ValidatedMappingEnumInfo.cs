using Microsoft.CodeAnalysis;

using System.Collections.Generic;

namespace Mappit.Generator
{
    internal sealed record ValidatedMappingEnumInfo : ValidatedMappingInfo
    {
        private ValidatedMappingEnumInfo(
            string methodName,
            List<ValidatedMappingEnumMemberInfo> memberMappings,
            ITypeSymbol sourceEnumType,
            ITypeSymbol targetEnumType,
            SyntaxNode associatedSyntaxNode,
            bool isImplicitMapping,
            bool requiresPartialMethod)
            : base(
                  methodName,
                  sourceEnumType,
                  targetEnumType,
                  associatedSyntaxNode,
                  isImplicitMapping)
        {
            MemberMappings = memberMappings;
            RequiresGeneration = true;
            RequiresPartialMethod &= requiresPartialMethod;
        }

        public List<ValidatedMappingEnumMemberInfo> MemberMappings { get; }

        public static ValidatedMappingEnumInfo Implicit(
            MappingTypeInfo mapping,
            ITypeSymbol sourceEnumType,
            ITypeSymbol targetEnumType,
            List<ValidatedMappingEnumMemberInfo> memberMappings)
        {
            return new ValidatedMappingEnumInfo(
                BuildImplicitMappingMethodName(sourceEnumType, targetEnumType),
                memberMappings,
                sourceEnumType,
                targetEnumType,
                mapping.MethodDeclaration,
                true,
                mapping.RequiresPartialMethod);
        }

        public static ValidatedMappingEnumInfo Explicit(
            MappingTypeInfo mapping,
            List<ValidatedMappingEnumMemberInfo> memberMappings)
        {
            return new ValidatedMappingEnumInfo(
                mapping.MethodName,
                memberMappings,
                mapping.SourceType,
                mapping.TargetType,
                mapping.MethodDeclaration,
                false,
                mapping.RequiresPartialMethod);
        }
    }
}