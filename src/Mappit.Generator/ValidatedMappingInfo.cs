using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal abstract record ValidatedMappingInfo
    {
        protected ValidatedMappingInfo(
            string methodName, 
            ITypeSymbol sourceType, 
            ITypeSymbol targetType,
            SyntaxNode associatedSyntaxNode) 
        {
            MethodName = methodName;
            SourceType = sourceType;
            TargetType = targetType;
            MethodDeclaration = associatedSyntaxNode;
        }

        protected ValidatedMappingInfo(MappingTypeInfo mappingTypeInfo)
        {
            MethodName = mappingTypeInfo.MethodName;
            SourceType = mappingTypeInfo.SourceType;
            TargetType = mappingTypeInfo.TargetType;
            MethodDeclaration = mappingTypeInfo.MethodDeclaration;
            RequiresGeneration = mappingTypeInfo.RequiresGeneration;
            IsReverseMapping = mappingTypeInfo.IsReverseMapping;
            RequiresPartialMethod = !mappingTypeInfo.IsReverseMapping;
        }

        public string MethodName { get; init; }
        public ITypeSymbol SourceType { get; init; }
        public ITypeSymbol TargetType { get; init; }
        public SyntaxNode MethodDeclaration { get; init; }
        public bool RequiresGeneration { get; init; }
        public bool IsReverseMapping { get; init; }
        public bool RequiresPartialMethod { get; init; }
    }
}