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
            TargetType = mappingTypeInfo.TargetType;
            FieldDeclaration = mappingTypeInfo.FieldDeclaration;

            MappingImplementationTypeName = $"{FieldName}_{SourceType.Name}_{TargetType.Name}_Mapping";
        }

        public string FieldName { get; }
        public ITypeSymbol SourceType { get; }
        public ITypeSymbol TargetType { get; }
        public SyntaxNode FieldDeclaration { get; }
        internal string MappingImplementationTypeName { get; }
    }
}