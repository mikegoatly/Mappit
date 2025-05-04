using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    public partial class MappitGenerator : IIncrementalGenerator
    {
        private static void ValidateMappings(SourceProductionContext context, MapperClassInfo mapperClass)
        {
            foreach (var mapping in mapperClass.Mappings)
            {
                if (mapping.ValidationError != MappingTypeValidationError.None)
                {
                    ReportDiagnostic(
                        context,
                        $"Mapping error for '{mapping.SourceType}' to '{mapping.DestinationType}': {mapping.ValidationError}",
                        mapping.FieldDeclaration);

                    continue;
                }

                Action<SourceProductionContext, MappingTypeInfo, MappingMemberInfo> validationMethod = mapping.IsEnum 
                    ? ValidateEnumMapping 
                    : ValidatePropertyMapping;

                foreach (var propertyMapping in mapping.PropertyMappings)
                {
                    validationMethod(context, mapping, propertyMapping);
                }
            }
        }

        private static void ValidateEnumMapping(SourceProductionContext context, MappingTypeInfo mapping, MappingMemberInfo enumMapping)
        {
            bool sourceValueExists = false;
            bool targetValueExists = false;

            // Find all enum types in the source type
            foreach (var enumMember in mapping.SourceType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.IsStatic && f.IsConst || f.IsReadOnly))
            {
                if (enumMember.Name == enumMapping.SourceName)
                {
                    sourceValueExists = true;
                    break;
                }
            }

            // Find all enum types in the destination type
            foreach (var enumMember in mapping.DestinationType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.IsStatic && f.IsConst || f.IsReadOnly))
            {
                if (enumMember.Name == enumMapping.TargetName)
                {
                    targetValueExists = true;
                    break;
                }
            }

            // Report diagnostics if values don't exist
            if (!sourceValueExists)
            {
                ReportDiagnostic(context,
                    $"Source enum value '{enumMapping.SourceName}' not found in any enum property of type '{mapping.SourceType.Name}'",
                    enumMapping.SourceArgument);
            }

            if (!targetValueExists)
            {
                ReportDiagnostic(context,
                    $"Target enum value '{enumMapping.TargetName}' not found in any enum property of type '{mapping.DestinationType.Name}'",
                    enumMapping.TargetArgument);
            }
        }

        private static void ValidatePropertyMapping(SourceProductionContext context, MappingTypeInfo mapping, MappingMemberInfo propertyMapping)
        {
            var sourceProperty = mapping.SourceType.GetMembers().OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name == propertyMapping.SourceName);

            var targetProperty = mapping.DestinationType.GetMembers().OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name == propertyMapping.TargetName);

            // Report diagnostics if properties don't exist
            if (sourceProperty == null)
            {
                ReportDiagnostic(context,
                    $"Source property '{propertyMapping.SourceName}' not found in type '{mapping.SourceType.Name}'",
                    propertyMapping.SourceArgument);
            }

            if (targetProperty == null)
            {
                ReportDiagnostic(context,
                    $"Target property '{propertyMapping.TargetName}' not found in type '{mapping.DestinationType.Name}'",
                    propertyMapping.TargetArgument);
            }

            // Check if property types are compatible
            if (sourceProperty != null && targetProperty != null)
            {
                bool isCompatible = false;

                // Direct type compatibility
                if (sourceProperty.Type.Equals(targetProperty.Type, SymbolEqualityComparer.Default))
                {
                    isCompatible = true;
                }
                // Both are enums
                else if (sourceProperty.Type.TypeKind == TypeKind.Enum && targetProperty.Type.TypeKind == TypeKind.Enum)
                {
                    isCompatible = true;
                }

                if (!isCompatible && !targetProperty.IsWriteOnly && !sourceProperty.IsReadOnly)
                {
                    ReportDiagnostic(context,
                        $"Incompatible types for property mapping: '{sourceProperty.Type.Name}' to '{targetProperty.Type.Name}'",
                        propertyMapping.AttributeSyntaxNode);
                }
            }
        }

        // TODO need to specialize error ids for each error type
        private static void ReportDiagnostic(SourceProductionContext context, string message, SyntaxNode node)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "MAPPIT001",
                title: "Mappit Mapping Error",
                messageFormat: message,
                category: "Mappit",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            if (node != null)
            {
                // Get the most precise span possible for better error location reporting
                var location = Location.Create(node.SyntaxTree, node.Span);
                context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
            }
        }
    }
}
