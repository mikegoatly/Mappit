using Microsoft.CodeAnalysis;

using System;

namespace Mappit.Generator
{
    internal abstract class ValidatedMappingInfo
    {
        protected ValidatedMappingInfo(MappingTypeInfo mappingTypeInfo)
        {
            FieldName = mappingTypeInfo.FieldName;
            SourceType = mappingTypeInfo.SourceType;
            DestinationType = mappingTypeInfo.DestinationType;
            FieldDeclaration = mappingTypeInfo.FieldDeclaration;

            MappingImplementationTypeName = $"{FieldName}_{SourceType.Name}_{DestinationType.Name}_Mapping";
        }

        public string FieldName { get; }
        public ITypeSymbol SourceType { get; }
        public ITypeSymbol DestinationType { get; }
        public SyntaxNode FieldDeclaration { get; }
        internal string MappingImplementationTypeName { get; }
    }
}