using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mappit.Generator
{
    /// <summary>
    /// Information about a mapping declaration
    /// </summary>
    internal sealed record MappingTypeInfo
    {
        private readonly IMethodSymbol _methodSymbol;

        public MappingTypeInfo(IMethodSymbol methodSymbol, ITypeSymbol sourceType, ITypeSymbol targetType, MethodDeclarationSyntax methodDeclaration)
        {
            _methodSymbol = methodSymbol;
            SourceType = sourceType;
            TargetType = targetType;
            MethodDeclaration = methodDeclaration;
            RequiresPartialMethod = true;
            IsEnum = sourceType.IsEnum() || targetType.IsEnum();

            if (IsEnum)
            {
                if (!sourceType.IsEnum() || !targetType.IsEnum())
                {
                    ValidationErrors.Add((MappitErrorCode.EnumTypeMismatch, $"{(sourceType.IsEnum() ? "source type" : "target type")} is an enum, but the other is not."));
                }
            }
        }

        public List<(MappitErrorCode, string)> ValidationErrors { get; set; } = [];
        public bool RequiresGeneration => _methodSymbol.IsPartialDefinition;
        public bool IsEnum { get; }
        public string MethodName => _methodSymbol.Name;

        public ITypeSymbol SourceType { get; init; }
        public ITypeSymbol TargetType { get; init; }
        public SyntaxNode MethodDeclaration { get; init; }

        /// <summary>
        /// Whether to ignore missing properties on the target type.
        /// This combines the class-level setting with any method-level override.
        /// </summary>
        public bool IgnoreMissingPropertiesOnTarget { get; set; }

        /// <summary>
        /// The member mappings for the source and target types. Keyed by the source member name.
        /// </summary>
        public Dictionary<string, MappingMemberInfo> PropertyMappings { get; init; } = new();

        /// <summary>
        /// The member ma0
        /// </summary>
        public Dictionary<string, MappingMemberInfo> EnumValueMappings { get; init; } = new();

        public bool RequiresPartialMethod { get; init; }

        public MappingTypeInfo BuildReverseMapping()
        {
            // We don't copy any current validation error otherwise it will be duplicated in the reverse mapping
            // But we do need to check that this isn't a custom mapping that can't be reversed.
            List<(MappitErrorCode, string)> validationErrors = _methodSymbol.IsPartialDefinition
                ? []
                : [(MappitErrorCode.CannotReverseMapCustomMapping, $"Cannot reverse map a custom mapping.")];

            if (PropertyMappings.Values.Any(p => p.ValueConversionMethod is not null))
            {
                validationErrors.Add((MappitErrorCode.CannotReverseMapCustomMapping, $"Cannot reverse map a mapping with a custom property mapping"));
            }

            return this with
            {
                SourceType = TargetType,
                TargetType = SourceType,
                RequiresPartialMethod = false,
                ValidationErrors = validationErrors,

                // Reverse the member mappings, remembering to change the key to the target name.
                PropertyMappings = PropertyMappings.Values.ToDictionary(
                    v => v.TargetName,
                    v => v with
                    {
                        SourceName = v.TargetName,
                        TargetName = v.SourceName,
                        SyntaxNode = v.SyntaxNode,
                        SourceArgument = v.TargetArgument,
                        TargetArgument = v.SourceArgument,
                    }),

                EnumValueMappings = EnumValueMappings.Values.ToDictionary(
                    v => v.TargetName,
                    v => v with
                    {
                        SourceName = v.TargetName,
                        TargetName = v.SourceName,
                        SyntaxNode = v.SyntaxNode,
                        SourceArgument = v.TargetArgument,
                        TargetArgument = v.SourceArgument,
                    }),
            };
        }
    }
}