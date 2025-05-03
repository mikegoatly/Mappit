using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    /// <summary>
    /// Information about a mapper class
    /// </summary>
    internal sealed class MapperClassInfo
    {
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public INamedTypeSymbol Symbol { get; set; }
        public List<MappingTypeInfo> Mappings { get; } = new List<MappingTypeInfo>();
    }
}