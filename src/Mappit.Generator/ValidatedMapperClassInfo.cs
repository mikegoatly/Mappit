using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal sealed class ValidatedMapperClassInfo
    {
        public ValidatedMapperClassInfo(MapperClassInfo classInfo)
        {
            ClassName = classInfo.ClassName;
            Namespace = classInfo.Namespace;
            Symbol = classInfo.Symbol;
        }

        public string ClassName { get; }
        public string Namespace { get; }
        public INamedTypeSymbol Symbol { get; }
        public List<ValidatedMappingTypeInfo> TypeMappings { get; } = new();
        public List<ValidatedMappingEnumInfo> EnumMappings { get; } = new();

        internal bool TryGetMappedType(ITypeSymbol sourceType, ITypeSymbol targetType, out ValidatedMappingInfo? typeMapping)
        {
            foreach (var mapping in TypeMappings.Concat<ValidatedMappingInfo>(EnumMappings))
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
    }
}