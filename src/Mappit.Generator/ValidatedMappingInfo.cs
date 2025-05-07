using Microsoft.CodeAnalysis;

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
            IsReverseMapping = mappingTypeInfo.IsReverseMapping;
        }

        public string MethodName { get; }
        public ITypeSymbol SourceType { get; }
        public ITypeSymbol TargetType { get; }
        public SyntaxNode MethodDeclaration { get; }
        public bool RequiresGeneration { get; }
        public bool IsReverseMapping { get; }
        public bool RequiresPartialMethod => !this.IsReverseMapping;
    }
}