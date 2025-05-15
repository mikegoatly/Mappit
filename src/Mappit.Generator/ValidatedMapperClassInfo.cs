using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal sealed record ValidatedMapperClassInfo : MapperClassInfoBase
    {
        public ValidatedMapperClassInfo(MapperClassInfo classInfo)
            : base(classInfo.Symbol)
        {
        }

        public List<ValidatedMappingTypeInfo> TypeMappings { get; } = new();
        public List<ValidatedMappingEnumInfo> EnumMappings { get; } = new();
        public List<ValidatedCollectionMappingTypeInfo> CollectionMappings { get; } = new();
        public List<ValidatedNullableMappingTypeInfo> NullableMappings { get; } = new();

        internal bool TryGetMappedType(ITypeSymbol sourceType, ITypeSymbol targetType, out ValidatedMappingInfo? typeMapping)
        {
            foreach (var mapping in TypeMappings
                .Concat<ValidatedMappingInfo>(EnumMappings)
                .Concat(CollectionMappings)
                .Concat(NullableMappings))
            {
                if (mapping.SourceType.Equals(sourceType, SymbolEqualityComparer.Default) &&
                    mapping.TargetType.Equals(targetType, SymbolEqualityComparer.Default))
                {
                    typeMapping = mapping;
                    return true;
                }
            }

            typeMapping = default;
            return false;
        }

        public override bool HasHapping(ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            return TryGetMappedType(sourceType, targetType, out _);
        }
    }
}