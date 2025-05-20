using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal sealed record ValidatedMappingTypeInfo : ValidatedMappingInfo
    {
        private ValidatedMappingTypeInfo(
            string methodName,
            ValidatedMappingMemberInfoSet memberMappings,
            ITypeSymbol sourceType,
            ITypeSymbol targetType,
            IMethodSymbol? constructor,
            bool isImplicitMapping,
            MappingTypeInfo mapping)
            : base(
                  methodName,
                  sourceType,
                  targetType,
                  mapping.MethodDeclaration,
                  isImplicitMapping)
        {
            Constructor = constructor;
            MemberMappings = memberMappings;
            RequiresGeneration = mapping.RequiresGeneration;
            RequiresPartialMethod &= mapping.RequiresPartialMethod;
        }

        /// <summary>
        /// A lookup of the source and target member mappings. Keyed by the target member name.
        /// </summary>
        /// <remarks>
        /// We use an ordinal ignore case comparer so we can match property names to parameter names of constructors.
        /// However, this will mean that if a property is named "Name" and another property is named "name" we will
        /// not be able to distinguish between them.
        /// </remarks>
        public ValidatedMappingMemberInfoSet MemberMappings { get; }

        public IMethodSymbol? Constructor { get; }

        public static ValidatedMappingTypeInfo Implicit(
            MappingTypeInfo mapping,
            ITypeSymbol sourceEnumType,
            ITypeSymbol targetEnumType,
            ValidatedMappingMemberInfoSet memberMappings,
            IMethodSymbol constructor)
        {
            return new ValidatedMappingTypeInfo(
                BuildImplicitMappingMethodName(sourceEnumType, targetEnumType),
                memberMappings,
                sourceEnumType,
                targetEnumType,
                constructor,
                true,
                mapping);
        }

        public static ValidatedMappingTypeInfo Explicit(
            MappingTypeInfo mapping,
            ValidatedMappingMemberInfoSet memberMappings,
            IMethodSymbol constructor)
        {
            return new ValidatedMappingTypeInfo(
                mapping.MethodName,
                memberMappings,
                mapping.SourceType,
                mapping.TargetType,
                constructor,
                false,
                mapping);
        }

        public static ValidatedMappingTypeInfo ExplicitUserImplemented(
           MappingTypeInfo mapping)
        {
            return new ValidatedMappingTypeInfo(
                mapping.MethodName,
                [],
                mapping.SourceType,
                mapping.TargetType,
                null,
                false,
                mapping);
        }
    }
}