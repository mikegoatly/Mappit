using Microsoft.CodeAnalysis;

using System;

namespace Mappit.Generator
{
    internal abstract class ValidatedMappingInfo
    {
        protected ValidatedMappingInfo(MappingTypeInfo mappingTypeInfo)
        {
            MethodName = mappingTypeInfo.MethodName;
            SourceType = mappingTypeInfo.SourceType;
            TargetType = mappingTypeInfo.TargetType;
            MethodDeclaration = mappingTypeInfo.MethodDeclaration;
            RequiresGeneration = mappingTypeInfo.RequiresGeneration;
        }

        public string MethodName { get; }
        public ITypeSymbol SourceType { get; }
        public ITypeSymbol TargetType { get; }
        public SyntaxNode MethodDeclaration { get; }
        public bool RequiresGeneration { get; }
    }
}