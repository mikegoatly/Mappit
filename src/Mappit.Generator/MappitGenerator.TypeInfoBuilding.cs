using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var mapperClass = CreateMapperClassInfo(classDeclarationSyntax, context, classSymbol);

            // Find partial methods starting with "Map"
            foreach (var member in classSymbol.GetMembers())
            {
                // TODO report warnings for "Map*" methods that don't match the pattern

                if (member is IMethodSymbol methodSymbol &&
                    methodSymbol.Name.StartsWith("Map", StringComparison.Ordinal) &&
                    methodSymbol.Parameters.Length == 1 &&
                    methodSymbol.ReturnType is ITypeSymbol destType)
                {
                    // Find the method declaration in the syntax tree
                    var methodDeclaration = methodSymbol.DeclaringSyntaxReferences
                        .Select(r => r.GetSyntax())
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault();

                    if (methodDeclaration == null)
                    {
                        continue;
                    }

                    var sourceType = methodSymbol.Parameters[0].Type;
                    var mappingInfo = BuildMapping(classSymbol, semanticModel, methodDeclaration, methodSymbol, sourceType, destType);

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
        }

        private static void ApplyMappingConfigForType(MapperClassInfo mapperClass, IMethodSymbol methodSymbol, MappingTypeInfo mappingInfo)
        {
            mappingInfo.IgnoreMissingPropertiesOnTarget = AttributeHasTrueValue(
                methodSymbol,
                nameof(IgnoreMissingPropertiesOnTargetAttribute),
                mapperClass.IgnoreMissingPropertiesOnTarget);

            mappingInfo.DeepCopyCollectionsAndDictionaries = AttributeHasTrueValue(
                methodSymbol,
                nameof(DeepCopyCollectionsAndDictionariesAttribute),
                mapperClass.DeepCopyCollectionsAndDictionaries);
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

        private static MapperClassInfo CreateMapperClassInfo(
            ClassDeclarationSyntax classDeclarationSyntax,
            GeneratorAttributeSyntaxContext context,
            INamedTypeSymbol classSymbol)
        {
            var mapperClass = new MapperClassInfo(classDeclarationSyntax, classSymbol);

            var ignoreMissingProperties = false;
            var deepCopyCollectionsAndDictionaries = false;
            var mappitAttribute = context.Attributes.FirstOrDefault();
            if (mappitAttribute != null)
            {
                foreach (var namedArg in mappitAttribute.NamedArguments)
                {
                    if (namedArg.Value.Value is bool value)
                    {
                        switch (namedArg.Key)
                        {
                            case nameof(MappitAttribute.IgnoreMissingPropertiesOnTarget):
                                ignoreMissingProperties = value;
                                break;
                            case nameof(MappitAttribute.DeepCopyCollectionsAndDictionaries):
                                deepCopyCollectionsAndDictionaries = value;
                                break;

                        }
                    }

                }
            }

            mapperClass.IgnoreMissingPropertiesOnTarget = ignoreMissingProperties;
            mapperClass.DeepCopyCollectionsAndDictionaries = deepCopyCollectionsAndDictionaries;
            return mapperClass;
        }

        private static MappingTypeInfo BuildMapping(
            INamedTypeSymbol classSymbol,
            SemanticModel semanticModel,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            ITypeSymbol sourceType,
            ITypeSymbol destType)
        {
            // Start building the mapping info for the source to target type
            var mappingInfo = new MappingTypeInfo(methodSymbol, sourceType, destType, methodDeclaration);
            var propertyMappingAttributeSyntaxes = GetAttributes(methodDeclaration, "MapProperty");

            // Process each attribute syntax
            foreach (var attr in propertyMappingAttributeSyntaxes)
            {
                var sourcePropertyName = attr.GetArgumentString(semanticModel, nameof(MapPropertyAttribute.SourceName), 0);
                if (sourcePropertyName is null)
                {
                    Debug.WriteLine($"Unable to locate {nameof(MapPropertyAttribute.SourceName)} argument for property mapping");
                    continue;
                }

                var targetPropertyName = attr.GetArgumentString(semanticModel, nameof(MapPropertyAttribute.TargetName), 1)
                    ?? sourcePropertyName;

                //if (attr.GetArgumentString(semanticModel, nameof(MapPropertyAttribute.TargetName)) is null)
                //{
                //    System.Diagnostics.Debugger.Launch();
                //}

                var memberMappingInfo = new MappingMemberInfo(sourcePropertyName, targetPropertyName, attr);

                if (attr.GetArgumentString(semanticModel, nameof(MapPropertyAttribute.ValueConversionMethod)) is { } conversionMethod)
                {
                    // Attempt to get the method symbol for the conversion method
                    var conversionMethodSymbol = classSymbol.GetMembers()
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(m => m.Name == conversionMethod);

                    if (conversionMethodSymbol is null)
                    {
                        mappingInfo.ValidationErrors.Add(
                            (MappitErrorCode.ConversionMethodNotFound, $"Unable to locate method '{conversionMethod}' for converting property mapping '{sourcePropertyName}'."));
                    }
                    else
                    {
                        memberMappingInfo.ValueConversionMethod = conversionMethodSymbol;
                    }
                }

                mappingInfo.PropertyMappings.Add(sourcePropertyName, memberMappingInfo);
            }

            var enumMappingAttributeSyntaxes = GetAttributes(methodDeclaration, "MapEnumValue");

            foreach (var attr in enumMappingAttributeSyntaxes)
            {
                var sourceEnumValue = attr.GetArgumentString(semanticModel, nameof(MapEnumValueAttribute.SourceName), 0);
                var targetEnumValue = attr.GetArgumentString(semanticModel, nameof(MapEnumValueAttribute.TargetName), 1);

                if (sourceEnumValue is null || targetEnumValue is null)
                {
                    Debug.WriteLine("Unable to locate SourceName or TargetName argument for enum value mapping");
                    continue;
                }

                mappingInfo.EnumValueMappings.Add(
                    sourceEnumValue,
                    new MappingMemberInfo(sourceEnumValue, targetEnumValue, attr));
            }

            return mappingInfo;
        }

        private static List<AttributeSyntax> GetAttributes(MethodDeclarationSyntax methodDeclaration, string attributeName)
        {
            return methodDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Where(a => a.Name.ToString() == attributeName)
                .ToList();
        }
    }
}
