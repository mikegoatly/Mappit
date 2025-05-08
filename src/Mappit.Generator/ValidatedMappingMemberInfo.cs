using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal enum TargetMappingKind
    {
        None,
        Constructor,
        Initialization
    }

    internal class ValidatedMappingMemberInfo
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
        public TargetMappingKind TargetMapping { get; internal set; }
        
        /// <summary>
        /// The kind of mapping to perform for this property, specifically whether 
        /// it is a standard property mapping, a collection mapping, or a dictionary mapping.
        /// </summary>
        public PropertyKind PropertyMappingKind { get; internal set; }

        /// <summary>
        /// For collection or dictionary mappings, the type of elements in the source and target collection
        /// </summary>
        public (ITypeSymbol sourceElementType, ITypeSymbol targetElementType)? ElementTypeMap { get; internal set; }
        
        /// <summary>
        /// For dictionary mappings, the type of keys in the source and target dictionary
        /// </summary>
        public (ITypeSymbol sourceKeyType, ITypeSymbol targetKeyType)? KeyTypeMap { get; internal set; }

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