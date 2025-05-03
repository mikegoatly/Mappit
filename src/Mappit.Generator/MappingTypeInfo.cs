using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    /// <summary>
    /// Information about a mapping declaration
    /// </summary>
    internal sealed class MappingTypeInfo
    {
        public MappingTypeInfo(string fieldName, ITypeSymbol sourceType, ITypeSymbol destinationType)
        {
            FieldName = fieldName;
            SourceType = sourceType;
            DestinationType = destinationType;
        }

        public string FieldName { get; }
        public ITypeSymbol SourceType { get; }
        public ITypeSymbol DestinationType { get; }
        public List<MappingMemberInfo> EnumMappings { get; } = new();
        public List<MappingMemberInfo> PropertyMappings { get; } = new();
    }
}