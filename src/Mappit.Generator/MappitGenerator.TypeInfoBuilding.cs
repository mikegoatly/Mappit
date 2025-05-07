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
            //try
            //{
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

                // Find partial methods starting with "Map"
                foreach (var member in classSymbol.GetMembers())
                {
                    // TODO report warnings for "Map*" methods that don't match the pattern

                    if (member is IMethodSymbol methodSymbol &&
                        methodSymbol.Name.StartsWith("Map", StringComparison.Ordinal) &&
                        methodSymbol.Parameters.Length == 1 &&
                        methodSymbol.ReturnType is ITypeSymbol returnType)
                    {
                        // Find the method declaration in the syntax tree
                        var methodDeclaration = classDeclarationSyntax.DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault(m => m.Identifier.Text == methodSymbol.Name);

                        if (methodDeclaration == null)
                        {
                            continue;
                        }

                        var sourceType = methodSymbol.Parameters[0].Type;
                        var destType = returnType;

                        var mappingInfo = BuildMapping(semanticModel, methodDeclaration, methodSymbol, sourceType, destType);

                        // Apply any configuration attributes applied to the method, combined with the class-level settings
                        ApplyMappingConfigForType(mapperClass, methodSymbol, mappingInfo);

                        mapperClass.Mappings.Add(mappingInfo);

                        if (methodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(ReverseMapAttribute)))
                        {
                            // Create a reverse mapping for the method
                            mapperClass.Mappings.Add(mappingInfo.BuildReverseMapping());
                        }
                    }
                }

                return mapperClass.Mappings.Count > 0 ? mapperClass : null;
//            }
//            catch (Exception ex)
//            {
//#if DEBUG
//                System.Diagnostics.Debugger.Launch();
//                return null;
//#endif
//            }
        }

        private static void ApplyMappingConfigForType(MapperClassInfo mapperClass, IMethodSymbol methodSymbol, MappingTypeInfo mappingInfo)
        {
            mappingInfo.IgnoreMissingPropertiesOnTarget = AttributeHasTrueValue(
                methodSymbol,
                nameof(IgnoreMissingPropertiesOnTargetAttribute),
                mapperClass.IgnoreMissingPropertiesOnTarget);
        }

        private static bool AttributeHasTrueValue(IMethodSymbol methodSymbol, string attributeName, bool defaultValue)
        {
            // Check if the method has the specified attribute and if it has a true value
            var attribute = methodSymbol.GetAttributes()
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

        private static MappingTypeInfo BuildMapping(SemanticModel semanticModel, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, ITypeSymbol sourceType, ITypeSymbol destType)
        {
            // Start building the mapping info for the source to target type
            var mappingInfo = new MappingTypeInfo(methodSymbol, sourceType, destType, methodDeclaration);

            // Find all MapMember attributes in the syntax tree - these will be the custom mappings
            var memberMappingAttributeSyntaxes = methodDeclaration.AttributeLists
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
