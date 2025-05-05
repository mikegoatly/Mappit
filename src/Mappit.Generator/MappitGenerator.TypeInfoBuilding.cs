using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

            // Check if the class inherits from MapperBase
            // TODO move all the logic from MapperBase to the generated class - that will remove the need to derive from it all the time.
            if (classSymbol?.BaseType?.Name != "MapperBase")
            {
                return null;
            }

            var mapperClass = new MapperClassInfo(classSymbol);

            // Find fields with TypeMapping<TSource, TDestination> type
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
                    mapperClass.Mappings.Add(mappingInfo);
                }
            }

            return mapperClass.Mappings.Count > 0 ? mapperClass : null;
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
