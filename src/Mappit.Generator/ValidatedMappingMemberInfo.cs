using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal enum PropertyMappingKind
    {
        None,
        Constructor,
        Initialization
    }

    internal sealed class ValidatedMappingMemberInfo
    {
        public ValidatedMappingMemberInfo(IPropertySymbol sourceProperty, IPropertySymbol targetProperty)
        {
            SourceProperty = sourceProperty;
            TargetProperty = targetProperty;
        }

        public IPropertySymbol SourceProperty { get; }
        public IPropertySymbol TargetProperty { get; }
        public PropertyMappingKind MappingKind { get; internal set; }
    }
}