using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mappit.Generator
{
    /// <summary>
    /// Information about a mapper class
    /// </summary>
    internal sealed record MapperClassInfo : MapperClassInfoBase
    {
        public MapperClassInfo(ClassDeclarationSyntax classDeclarationSyntax, INamedTypeSymbol symbol)
            : base(symbol)
        {
            this.ClassDeclarationSyntax = classDeclarationSyntax;
        }

        public bool DeepCopyCollectionsAndDictionaries { get; internal set; }
        public bool IgnoreMissingPropertiesOnTarget { get; internal set; }
        public List<MappingTypeInfo> Mappings { get; } = new();
        public ClassDeclarationSyntax ClassDeclarationSyntax { get; }

        public override bool HasHapping(ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            foreach (var mapping in Mappings)
            {
                if (mapping.SourceType.Equals(sourceType, SymbolEqualityComparer.Default) &&
                    mapping.TargetType.Equals(targetType, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }

            return false;
        }
    }
}