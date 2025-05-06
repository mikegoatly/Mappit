using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace Mappit.Generator
{
    public partial class MappitGenerator : IIncrementalGenerator
    {
        private static MapperClassInfo? GetMapperClassInfo(GeneratorAttributeSyntaxContext context)
        {
            if (context.TargetNode is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return null;
            }

            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
            if (classSymbol is null)
            {
                return null;
            }

            // Create the mapper class info with the class-level config
            var mapperClass = CreateMapperClassInfo(context, classSymbol);

            // Find fields with TypeMapping<TSource, TTarget> type
            foreach (var member in classSymbol.GetMembers())
            {
                if (member is IFieldSymbol fieldSymbol &&
                    fieldSymbol.Type is INamedTypeSymbol namedType &&
                    namedType.IsGenericType &&
                    namedType.Name == "TypeMapping" &&
                    namedType.TypeArguments.Length == 2 &&
                    namedType.TypeArguments[0] is ITypeSymbol sourceType &&
                    namedType.TypeArguments[1] is ITypeSymbol destType)
                {
                    // Find the field declaration in the syntax tree
                    var fieldDeclarations = classDeclarationSyntax.DescendantNodes()
                        .OfType<FieldDeclarationSyntax>()
                        .Where(f => f.Declaration.Variables.Any(v => v.Identifier.Text == fieldSymbol.Name))
                        .FirstOrDefault();

                    if (fieldDeclarations is not { } fieldDeclaration)
                    {
                        continue;
                    }

                    var mappingInfo = BuildMapping(semanticModel, fieldDeclaration, fieldSymbol, sourceType, destType);

                    // Apply any configuration attributes applied to the field, combined with the class-level settings
                    ApplyMappingConfigForType(mapperClass, fieldSymbol, mappingInfo);

                    mapperClass.Mappings.Add(mappingInfo);
                }
            }

            return mapperClass.Mappings.Count > 0 ? mapperClass : null;
        }

        private static void ApplyMappingConfigForType(MapperClassInfo mapperClass, IFieldSymbol fieldSymbol, MappingTypeInfo mappingInfo)
        {
            mappingInfo.IgnoreMissingPropertiesOnTarget = AttributeHasTrueValue(
                fieldSymbol,
                nameof(IgnoreMissingPropertiesOnTargetAttribute),
                mapperClass.IgnoreMissingPropertiesOnTarget);
        }

        private static bool AttributeHasTrueValue(IFieldSymbol fieldSymbol, string attributeName, bool defaultValue)
        {
            // Check if the field has the specified attribute and if it has a true value
            var attribute = fieldSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

            if (attribute != null && attribute.ConstructorArguments.Length > 0)
            {
                return attribute.ConstructorArguments[0].Value is bool value && value;
            }

            return defaultValue;
        }

        private static MapperClassInfo CreateMapperClassInfo(GeneratorAttributeSyntaxContext context, INamedTypeSymbol classSymbol)
        {
            var mapperClass = new MapperClassInfo(classSymbol);

            var ignoreMissingProperties = false;
            var mappitAttribute = context.Attributes.FirstOrDefault();
            if (mappitAttribute != null)
            {
                foreach (var namedArg in mappitAttribute.NamedArguments)
                {
                    if (namedArg.Key == nameof(MappitAttribute.IgnoreMissingPropertiesOnTarget) && namedArg.Value.Value is bool value)
                    {
                        ignoreMissingProperties = value;
                        break;
                    }
                }
            }

            mapperClass.IgnoreMissingPropertiesOnTarget = ignoreMissingProperties;
            return mapperClass;
        }

        private static MappingTypeInfo BuildMapping(SemanticModel semanticModel, FieldDeclarationSyntax fieldDeclaration, IFieldSymbol fieldSymbol, ITypeSymbol sourceType, ITypeSymbol destType)
        {
            // Start building the mapping info for the source to target type
            var mappingInfo = new MappingTypeInfo(fieldSymbol.Name, sourceType, destType, fieldDeclaration);

            // Find all MapMember attributes in the syntax tree - these will be the custom mappings
            var memberMappingAttributeSyntaxes = fieldDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(a => a.Name.ToString() == "MapMember" && a.ArgumentList?.Arguments.Count == 2)
                .ToList();

            // Process each attribute syntax
            foreach (var attrSyntax in memberMappingAttributeSyntaxes)
            {
                // TODO I think if someone has changed the order of the attributes by naming them this will break?
                var sourcePropertyName = GetArgumentStringValue(attrSyntax.ArgumentList!.Arguments[0], semanticModel);
                var targetPropertyName = GetArgumentStringValue(attrSyntax.ArgumentList!.Arguments[1], semanticModel);

                mappingInfo.MemberMappings.Add(
                    sourcePropertyName,
                    new MappingMemberInfo(sourcePropertyName, targetPropertyName, attrSyntax));
            }

            return mappingInfo;
        }

        private static string GetArgumentStringValue(AttributeArgumentSyntax argument, SemanticModel semanticModel)
        {
            // If it's a simple literal, just return its string representation
            if (argument.Expression is LiteralExpressionSyntax literal)
            {
                return literal.Token.ValueText;
            }

            // Otherwise, try to get the constant value from semantic model
            var constantValue = semanticModel.GetConstantValue(argument.Expression);
            if (constantValue.HasValue && constantValue.Value is string stringValue)
            {
                return stringValue;
            }

            // If we can't determine the value, return the expression text as a fallback
            return argument.Expression.ToString();
        }
    }
}
