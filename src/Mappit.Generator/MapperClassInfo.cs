using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    /// <summary>
    /// Information about a mapper class
    /// </summary>
    internal sealed class MapperClassInfo
    {
        public MapperClassInfo(INamedTypeSymbol symbol)
        {
            ClassName = symbol.Name;
            Namespace = symbol.ContainingNamespace.ToDisplayString();
            Symbol = symbol;
        }

        public string ClassName { get; }
        public string Namespace { get; }
        public INamedTypeSymbol Symbol { get; }
        public bool IgnoreMissingPropertiesOnTarget { get; internal set; }
        public List<MappingTypeInfo> Mappings { get; } = new();
    }
}