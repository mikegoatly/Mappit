using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mappit.Generator
{
    internal enum MappingTypeValidationError
    {
        None,

        /// <summary>
        /// The source type or target type is an enum, but the other type is not
        /// </summary>
        EnumTypeMismatch,
    }

    /// <summary>
    /// Information about a mapping declaration
    /// </summary>
    internal sealed class MappingTypeInfo
    {
        private readonly IMethodSymbol _methodSymbol;

        public MappingTypeInfo(IMethodSymbol methodSymbol, ITypeSymbol sourceType, ITypeSymbol targetType, MethodDeclarationSyntax methodDeclaration)
        {
            this._methodSymbol = methodSymbol;
            SourceType = sourceType;
            TargetType = targetType;
            MethodDeclaration = methodDeclaration;
            IsEnum = sourceType.TypeKind == TypeKind.Enum || targetType.TypeKind == TypeKind.Enum;

            if (IsEnum)
            {
                if (sourceType.TypeKind != TypeKind.Enum || targetType.TypeKind != TypeKind.Enum)
                {
                    ValidationError = MappingTypeValidationError.EnumTypeMismatch;
                }
            }
        }

        public MappingTypeValidationError ValidationError { get; set; } = MappingTypeValidationError.None;
        public bool RequiresGeneration => _methodSymbol.IsPartialDefinition;
        public bool IsEnum { get; }
        public string MethodName => _methodSymbol.Name;

        public ITypeSymbol SourceType { get; }
        public ITypeSymbol TargetType { get; }
        public SyntaxNode MethodDeclaration { get; }

        /// <summary>
        /// Whether to ignore missing properties on the target type.
        /// This combines the class-level setting with any method-level override.
        /// </summary>
        public bool IgnoreMissingPropertiesOnTarget { get; set; }

        /// <summary>
        /// The member mappings for the source and target types. Keyed by the source member name.
        /// </summary>
        public Dictionary<string, MappingMemberInfo> MemberMappings { get; } = new();
    }
}