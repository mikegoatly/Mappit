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
        public MappingTypeInfo(string fieldName, ITypeSymbol sourceType, ITypeSymbol destinationType, FieldDeclarationSyntax fieldDeclaration)
        {
            FieldName = fieldName;
            SourceType = sourceType;
            DestinationType = destinationType;
            this.FieldDeclaration = fieldDeclaration;
            IsEnum = sourceType.TypeKind == TypeKind.Enum || destinationType.TypeKind == TypeKind.Enum;

            if (IsEnum)
            {
                if (sourceType.TypeKind != TypeKind.Enum || destinationType.TypeKind != TypeKind.Enum)
                {
                    ValidationError = MappingTypeValidationError.EnumTypeMismatch;
                }
            }
        }

        public MappingTypeValidationError ValidationError { get; set; } = MappingTypeValidationError.None;
        public bool IsEnum { get; }
        public string FieldName { get; }
        public ITypeSymbol SourceType { get; }
        public ITypeSymbol DestinationType { get; }
        public SyntaxNode FieldDeclaration { get; }

        /// <summary>
        /// The member mappings for the source and destination types. Keyed by the source member name.
        /// </summary>
        public Dictionary<string, MappingMemberInfo> MemberMappings { get; } = new();
    }
}