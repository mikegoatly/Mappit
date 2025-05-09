using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal enum TargetMapping
    {
        None,
        Constructor,
        Initialization
    }

    internal sealed record ValidatedMappingMemberInfo
    {
        private ValidatedMappingMemberInfo(bool isValid, IPropertySymbol sourceProperty, IPropertySymbol targetProperty)
        {
            IsValid = isValid;
            SourceProperty = sourceProperty;
            TargetProperty = targetProperty;
        }

        /// <summary>
        /// We keep track of the mappings even if they aren't valid so we can generate placeholder code for them to reduce
        /// the number of compiler errors in the generated code.
        /// </summary>
        public bool IsValid { get; }
        public IPropertySymbol SourceProperty { get; }
        public IPropertySymbol TargetProperty { get; }
        public TargetMapping TargetMapping { get; internal set; }

        /// <summary>
        /// The concrete type that should be instantiated for a collection or dictionary.
        /// For example, if the target property is IEnumerable{T}, this would be List{T}.
        /// </summary>
        public string? ConcreteTargetType { get; internal set; }

        public static ValidatedMappingMemberInfo Invalid(IPropertySymbol sourceProperty, IPropertySymbol targetProperty)
        {
            return new ValidatedMappingMemberInfo(false, sourceProperty, targetProperty);
        }

        public static ValidatedMappingMemberInfo Valid(IPropertySymbol sourceProperty, IPropertySymbol targetProperty)
        {
            return new ValidatedMappingMemberInfo(true, sourceProperty, targetProperty);
        }
    }
}