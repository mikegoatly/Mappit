using Microsoft.CodeAnalysis;

using System.Text.RegularExpressions;

namespace Mappit.Generator
{
    internal abstract record ValidatedMappingInfo
    {
        private static readonly Regex invalidMethodCharacterReplacement = new(@"[^a-zA-Z0-9_匚コᐸᐳˀ]");

        protected ValidatedMappingInfo(
            string methodName, 
            ITypeSymbol sourceType, 
            ITypeSymbol targetType,
            SyntaxNode associatedSyntaxNode,
            bool isImplicitMapping) 
        {
            MethodName = methodName;
            SourceType = sourceType;
            TargetType = targetType;
            MethodDeclaration = associatedSyntaxNode;
            IsImplicitMapping = isImplicitMapping;
            RequiresPartialMethod = !isImplicitMapping;
        }

        protected ValidatedMappingInfo(MappingTypeInfo mappingTypeInfo)
        {
            MethodName = mappingTypeInfo.MethodName;
            SourceType = mappingTypeInfo.SourceType;
            TargetType = mappingTypeInfo.TargetType;
            MethodDeclaration = mappingTypeInfo.MethodDeclaration;
            RequiresGeneration = mappingTypeInfo.RequiresGeneration;
            RequiresPartialMethod = mappingTypeInfo.RequiresPartialMethod;
        }

        public string MethodName { get; init; }
        public ITypeSymbol SourceType { get; init; }
        public ITypeSymbol TargetType { get; init; }
        public SyntaxNode MethodDeclaration { get; init; }
        public bool RequiresGeneration { get; init; }
        public bool RequiresPartialMethod { get; init; }

        /// <summary>
        /// Gets whether this is an implicit (e.g. inferred as part of a mapping to a type's property)
        /// or an explicit mapping defined by the user's mapping type. Implicit mappings are not added to the mapper interface.
        /// </summary>
        public bool IsImplicitMapping { get; protected init; }

        protected static string BuildImplicitMappingMethodName(ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            return $"__Implicit_{FormatForMethodName(sourceType)}_to_{FormatForMethodName(targetType)}";
        }

        private static string FormatForMethodName(ITypeSymbol typeSymbol)
        {
            var name = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            name = name.Replace("[", "匚");
            name = name.Replace("]", "コ");
            name = name.Replace("<", "ᐸ");
            name = name.Replace(">", "ᐳ");
            name = name.Replace("?", "ˀ");

            // We'll likely have non-valid C# characters in the name, so we need to strip them out
            return invalidMethodCharacterReplacement.Replace(name, "_");
        }
    }
}