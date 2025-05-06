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
        public MappingTypeInfo(string fieldName, ITypeSymbol sourceType, ITypeSymbol targetType, FieldDeclarationSyntax fieldDeclaration)
        {
            FieldName = fieldName;
            SourceType = sourceType;
            TargetType = targetType;
            FieldDeclaration = fieldDeclaration;
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
        public bool IsEnum { get; }
        public string FieldName { get; }
        public ITypeSymbol SourceType { get; }
        public ITypeSymbol TargetType { get; }
        public SyntaxNode FieldDeclaration { get; }
        
        /// <summary>
        /// Whether to include compiler-generated properties in this mapping.
        /// This combines the class-level setting with any field-level override.
        /// </summary>
        public bool IncludeCompilerGenerated { get; set; }

        /// <summary>
        /// Whether to ignore missing properties on the target type.
        /// This combines the class-level setting with any field-level override.
        /// </summary>
        public bool IgnoreMissingPropertiesOnTarget { get; set; }

        /// <summary>
        /// The member mappings for the source and target types. Keyed by the source member name.
        /// </summary>
        public Dictionary<string, MappingMemberInfo> MemberMappings { get; } = new();
    }
}