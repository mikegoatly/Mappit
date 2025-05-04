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
    internal static class Attributes
    {
        public const string MappitAttribute = "Mappit.MappitAttribute";
    }

    [Generator]
    public partial class MappitGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    Attributes.MappitAttribute,
                    // Search for partial class declarations
                    predicate: static (s, _) => s is ClassDeclarationSyntax c && 
                        c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                    // The first transform grabs all the 
                    transform: static (ctx, _) => GetMapperClassInfo(ctx))
                .Where(static m => m != null);

            // Register the output generator
            context.RegisterSourceOutput(classDeclarations, GenerateMapper);
        }

        private static void GenerateMapper(SourceProductionContext context, MapperClassInfo? mapperClass)
        {
            if (mapperClass == null)
            {
                return;
            }

            ValidateMappings(context, mapperClass);

            var source = new StringBuilder();

            // Generate namespace start
            source.AppendLine($"namespace {mapperClass.Namespace}");
            source.AppendLine("{");

            // Generate partial class implementation
            source.AppendLine($"    public partial class {mapperClass.ClassName}");
            source.AppendLine("    {");

            // Generate constructor that initializes all mappings
            source.AppendLine($"        // Auto-generated constructor for {mapperClass.ClassName}");
            source.AppendLine($"        public {mapperClass.ClassName}()");
            source.AppendLine("        {");
            source.AppendLine("            // Initialize all mappings");

            // Generate mapping implementations and initialization
            foreach (var mapping in mapperClass.Mappings)
            {
                // Initialize and register each mapping
                source.AppendLine($"            {mapping.FieldName} = new {mapping.FieldName}Mapping();");
                source.AppendLine($"            RegisterMapping({mapping.FieldName});");
            }

            source.AppendLine("        }");

            // Generate mapping class implementations for each mapping declaration
            foreach (var mapping in mapperClass.Mappings)
            {
                GenerateMappingClass(source, mapping);
            }

            source.AppendLine("    }");
            source.AppendLine("}");

            // Add the source code to the compilation
            context.AddSource($"{mapperClass.ClassName}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }

        private static void GenerateMappingClass(StringBuilder source, MappingTypeInfo mapping)
        {
            var sourceTypeName = mapping.SourceType.Name;
            var destTypeName = mapping.DestinationType.Name;
            var fieldName = mapping.FieldName;

            source.AppendLine();
            source.AppendLine($"        // Implement {fieldName} mapping from {sourceTypeName} to {destTypeName}");
            source.AppendLine($"        private sealed class {fieldName}Mapping : Mappit.TypeMapping<{sourceTypeName}, {destTypeName}>");
            source.AppendLine("        {");
            source.AppendLine($"            public override {destTypeName} Map({sourceTypeName} typedSource)");
            source.AppendLine("            {");

            // Find constructors for destination type
            var ctors = mapping.DestinationType.GetMembers()
                .Where(m => m.Kind == SymbolKind.Method && m.Name == ".ctor")
                .Cast<IMethodSymbol>()
                .OrderByDescending(m => m.Parameters.Length)
                .ToList();

            var hasNonDefaultCtor = ctors.Any(c => c.Parameters.Length > 0);
            var hasDefaultCtor = ctors.Any(c => c.Parameters.Length == 0);

            // Start object initialization
            source.AppendLine($"                return new {destTypeName}");

            // If we have a parameterized constructor and no default constructor
            if (hasNonDefaultCtor && !hasDefaultCtor)
            {
                // Try to find the best constructor match
                var bestCtor = FindBestConstructorMatch(mapping, ctors);

                if (bestCtor != null && bestCtor.Parameters.Length > 0)
                {
                    // Generate constructor arguments
                    source.Append("                (");

                    for (int i = 0; i < bestCtor.Parameters.Length; i++)
                    {
                        var param = bestCtor.Parameters[i];

                        // Try to find a property in source that matches the parameter
                        var matchingProperty = FindMatchingSourceProperty(mapping, param);

                        if (matchingProperty != null)
                        {
                            // For enum parameters, check if we need to map them
                            if (param.Type.TypeKind == TypeKind.Enum && matchingProperty.Type.TypeKind == TypeKind.Enum)
                            {
                                source.Append($"MapEnum_{matchingProperty.Name}(typedSource.{matchingProperty.Name})");
                            }
                            else
                            {
                                source.Append($"typedSource.{matchingProperty.Name}");
                            }
                        }
                        else
                        {
                            // Use default value for the parameter type
                            source.Append($"default({param.Type})");
                        }

                        if (i < bestCtor.Parameters.Length - 1)
                        {
                            source.Append(", ");
                        }
                    }

                    source.AppendLine(")");
                }
                else
                {
                    // Fall back to default constructor if no good match found
                    source.AppendLine("                ()");
                }
            }
            else
            {
                // Use default constructor
                source.AppendLine("                ()");
            }

            // Collect all properties that were set via constructor
            var propertiesSetViaConstructor = new HashSet<string>();

            if (hasNonDefaultCtor && !hasDefaultCtor)
            {
                var bestCtor = FindBestConstructorMatch(mapping, ctors);
                if (bestCtor != null)
                {
                    foreach (var param in bestCtor.Parameters)
                    {
                        // Get matching dest property based on parameter name
                        var matchingDestProperty = mapping.DestinationType.GetMembers()
                            .OfType<IPropertySymbol>()
                            .FirstOrDefault(p => string.Equals(p.Name, param.Name, System.StringComparison.OrdinalIgnoreCase));

                        if (matchingDestProperty != null)
                        {
                            propertiesSetViaConstructor.Add(matchingDestProperty.Name);
                        }
                    }
                }
            }

            // Start object initializer
            source.AppendLine("                {");

            // Flag to track if we've added any properties in the initializer
            bool hasAddedProperty = false;

            // Handle custom property mappings first (skip those already set by constructor)
            foreach (var propertyMapping in mapping.PropertyMappings)
            {
                var sourceProperty = mapping.SourceType.GetMembers().OfType<IPropertySymbol>()
                    .FirstOrDefault(p => p.Name == propertyMapping.SourceName);

                var targetProperty = mapping.DestinationType.GetMembers().OfType<IPropertySymbol>()
                    .FirstOrDefault(p => p.Name == propertyMapping.TargetName);

                if (sourceProperty != null && targetProperty != null &&
                    !targetProperty.IsWriteOnly && !sourceProperty.IsReadOnly &&
                    !propertiesSetViaConstructor.Contains(targetProperty.Name))
                {
                    // Handle enum mappings
                    if (sourceProperty.Type.TypeKind == TypeKind.Enum && targetProperty.Type.TypeKind == TypeKind.Enum)
                    {
                        // Generate custom enum mapping
                        source.AppendLine($"                    // Custom mapping for enum property {propertyMapping.SourceName} to {propertyMapping.TargetName}");
                        source.AppendLine($"                    {propertyMapping.TargetName} = MapEnum_{propertyMapping.SourceName}_To_{propertyMapping.TargetName}(typedSource.{propertyMapping.SourceName}),");
                        hasAddedProperty = true;
                    }
                    else
                    {
                        // Regular property mapping
                        source.AppendLine($"                    // Custom mapping from {propertyMapping.SourceName} to {propertyMapping.TargetName}");
                        source.AppendLine($"                    {propertyMapping.TargetName} = typedSource.{propertyMapping.SourceName},");
                        hasAddedProperty = true;
                    }
                }
            }

            // Handle standard property mappings (excluding those already handled)
            var customMappedSourceProps = mapping.PropertyMappings.Select(p => p.SourceName).ToList();
            var customMappedTargetProps = mapping.PropertyMappings.Select(p => p.TargetName).ToList();

            // Generate property mappings based on shared property names
            foreach (var property in mapping.SourceType.GetMembers().OfType<IPropertySymbol>())
            {
                // Skip properties with custom mappings
                if (customMappedSourceProps.Contains(property.Name))
                    continue;

                var destProperty = mapping.DestinationType.GetMembers()
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(p => p.Name == property.Name &&
                                     !customMappedTargetProps.Contains(p.Name) &&
                                     !propertiesSetViaConstructor.Contains(p.Name) &&
                                     (p.Type.Equals(property.Type, SymbolEqualityComparer.Default) ||
                                      (property.Type.TypeKind == TypeKind.Enum && p.Type.TypeKind == TypeKind.Enum)));

                if (destProperty != null && !destProperty.IsReadOnly && !property.IsWriteOnly)
                {
                    if (property.Type.TypeKind == TypeKind.Enum && destProperty.Type.TypeKind == TypeKind.Enum)
                    {
                        // Generate standard enum mapping
                        source.AppendLine($"                    // Standard mapping for enum property {property.Name}");
                        source.AppendLine($"                    {property.Name} = MapEnum_{property.Name}(typedSource.{property.Name}),");
                        hasAddedProperty = true;
                    }
                    else
                    {
                        // Regular property mapping
                        source.AppendLine($"                    {property.Name} = typedSource.{property.Name},");
                        hasAddedProperty = true;
                    }
                }
            }

            // Remove trailing comma from the last property if any properties were added
            if (hasAddedProperty)
            {
                // Remove the last comma and newline
                source.Length -= 2;
                source.AppendLine();
            }

            // Close the object initializer
            source.AppendLine("                };");
            source.AppendLine("            }");

            // Generate enum mapping methods for each enum property
            GenerateEnumMappingMethods(source, mapping);

            source.AppendLine("        }");
        }

        private static IMethodSymbol FindBestConstructorMatch(MappingTypeInfo mapping, List<IMethodSymbol> constructors)
        {
            // First try exact match by number of source properties
            var sourceProperties = mapping.SourceType.GetMembers().OfType<IPropertySymbol>().ToList();

            foreach (var ctor in constructors)
            {
                int matchingParams = 0;

                foreach (var param in ctor.Parameters)
                {
                    // Try to find a source property that matches parameter name and type
                    var matchingProp = FindMatchingSourceProperty(mapping, param);
                    if (matchingProp != null)
                    {
                        matchingParams++;
                    }
                }

                // If all parameters can be matched, this is a good constructor choice
                if (matchingParams == ctor.Parameters.Length)
                {
                    return ctor;
                }
            }

            // If no perfect match, return the constructor with the most matching parameters
            return constructors
                .OrderByDescending(c => c.Parameters.Count(p => FindMatchingSourceProperty(mapping, p) != null))
                .FirstOrDefault();
        }

        private static IPropertySymbol? FindMatchingSourceProperty(MappingTypeInfo mapping, IParameterSymbol parameter)
        {
            var sourceProperties = mapping.SourceType.GetMembers().OfType<IPropertySymbol>().ToList();

            // First try direct name match
            var matchByName = sourceProperties.FirstOrDefault(p =>
                string.Equals(p.Name, parameter.Name, System.StringComparison.OrdinalIgnoreCase) &&
                (p.Type.Equals(parameter.Type, SymbolEqualityComparer.Default) ||
                 (p.Type.TypeKind == TypeKind.Enum && parameter.Type.TypeKind == TypeKind.Enum)));

            if (matchByName != null)
            {
                return matchByName;
            }

            // Check custom property mappings
            foreach (var customMapping in mapping.PropertyMappings)
            {
                var sourceProperty = sourceProperties.FirstOrDefault(p => p.Name == customMapping.SourceName);

                if (sourceProperty != null)
                {
                    var targetProperty = mapping.DestinationType.GetMembers().OfType<IPropertySymbol>()
                        .FirstOrDefault(p => p.Name == customMapping.TargetName);

                    if (targetProperty != null &&
                        string.Equals(targetProperty.Name, parameter.Name, System.StringComparison.OrdinalIgnoreCase) &&
                        (targetProperty.Type.Equals(parameter.Type, SymbolEqualityComparer.Default) ||
                         (targetProperty.Type.TypeKind == TypeKind.Enum && parameter.Type.TypeKind == TypeKind.Enum)))
                    {
                        return sourceProperty;
                    }
                }
            }

            return null;
        }

        private static void GenerateEnumMappingMethods(StringBuilder source, MappingTypeInfo mapping)
        {
            // Generate methods for custom property enum mappings
            foreach (var propertyMapping in mapping.PropertyMappings)
            {
                var sourceProperty = mapping.SourceType.GetMembers().OfType<IPropertySymbol>()
                    .FirstOrDefault(p => p.Name == propertyMapping.SourceName);

                var targetProperty = mapping.DestinationType.GetMembers().OfType<IPropertySymbol>()
                    .FirstOrDefault(p => p.Name == propertyMapping.TargetName);

                if (sourceProperty != null && targetProperty != null &&
                    sourceProperty.Type.TypeKind == TypeKind.Enum && targetProperty.Type.TypeKind == TypeKind.Enum)
                {
                    source.AppendLine();
                    source.AppendLine($"            private {targetProperty.Type.Name} MapEnum_{propertyMapping.SourceName}_To_{propertyMapping.TargetName}({sourceProperty.Type.Name} sourceValue)");
                    source.AppendLine("            {");
                    source.AppendLine("                switch (sourceValue)");
                    source.AppendLine("                {");

                    // Add custom enum mappings
                    var customMappings = mapping.EnumMappings
                        .Where(e => e.SourceName != null && e.TargetName != null);

                    foreach (var enumMapping in customMappings)
                    {
                        source.AppendLine($"                    case {sourceProperty.Type.Name}.{enumMapping.SourceName}:");
                        source.AppendLine($"                        return {targetProperty.Type.Name}.{enumMapping.TargetName};");
                    }

                    // Default case
                    source.AppendLine("                    default:");
                    source.AppendLine($"                        return ({targetProperty.Type.Name})sourceValue;");

                    source.AppendLine("                }");
                    source.AppendLine("            }");
                }
            }

            // Generate methods for standard enum properties
            var customMappedSourceProps = mapping.PropertyMappings.Select(p => p.SourceName).ToList();

            foreach (var property in mapping.SourceType.GetMembers().OfType<IPropertySymbol>())
            {
                // Skip properties with custom mappings
                if (customMappedSourceProps.Contains(property.Name))
                    continue;

                var destProperty = mapping.DestinationType.GetMembers()
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(p => p.Name == property.Name &&
                                     (property.Type.TypeKind == TypeKind.Enum && p.Type.TypeKind == TypeKind.Enum));

                if (destProperty != null && property.Type.TypeKind == TypeKind.Enum)
                {
                    source.AppendLine();
                    source.AppendLine($"            private {destProperty.Type.Name} MapEnum_{property.Name}({property.Type.Name} sourceValue)");
                    source.AppendLine("            {");
                    source.AppendLine("                switch (sourceValue)");
                    source.AppendLine("                {");

                    // Add any relevant custom enum mappings that might apply to this property
                    var relevantCustomMappings = mapping.EnumMappings
                        .Where(e => e.SourceName != null && e.TargetName != null);

                    foreach (var enumMapping in relevantCustomMappings)
                    {
                        source.AppendLine($"                    case {property.Type.Name}.{enumMapping.SourceName}:");
                        source.AppendLine($"                        return {destProperty.Type.Name}.{enumMapping.TargetName};");
                    }

                    // Default case - direct cast
                    source.AppendLine("                    default:");
                    source.AppendLine($"                        return ({destProperty.Type.Name})sourceValue;");

                    source.AppendLine("                }");
                    source.AppendLine("            }");
                }
            }
        }
    }
}