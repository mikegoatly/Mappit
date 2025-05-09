using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal abstract record MapperClassInfoBase
    {
        protected MapperClassInfoBase(INamedTypeSymbol symbol)
        {
            ClassName = symbol.Name;
            Namespace = symbol.ContainingNamespace.ToDisplayString();
            Symbol = symbol;
        }

        public string ClassName { get; }
        public string Namespace { get; }
        public INamedTypeSymbol Symbol { get; }

        public abstract bool HasHapping(ITypeSymbol sourceType, ITypeSymbol targetType);
    }
}