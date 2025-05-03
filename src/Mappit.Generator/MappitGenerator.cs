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
    [Generator]
    public class MappitGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Register syntaxes of interest - in this case, class declarations that are partial
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is ClassDeclarationSyntax c && c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                    transform: static (ctx, _) => GetMapperClassInfo(ctx))
                .Where(static m => m != null);

            // Register the output generator
            context.RegisterSourceOutput(classDeclarations, static (spc, mapper) => GenerateMapper(spc, mapper));
        }

        private static MapperClassInfo? GetMapperClassInfo(GeneratorSyntaxContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            // Check if the class inherits from MapperBase
            if (classSymbol?.BaseType?.Name != "MapperBase")
            {
                return null;
            }

            var mapperClass = new MapperClassInfo
            {
                ClassName = classSymbol.Name,
                Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
                Symbol = classSymbol
            };

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
                        .ToList();

                    if (fieldDeclarations.Count == 0)
                    {
                        continue;
                    }

                    var fieldDeclaration = fieldDeclarations.First();
                    var mappingInfo = new MappingTypeInfo(fieldSymbol.Name, sourceType, destType);

                    // Find any custom enum mappings
                    var enumMappingAttributes = fieldSymbol.GetAttributes()
                        .Where(a => a.AttributeClass?.Name == "MapEnumValueAttribute")
                        .ToList();

                    // Find all MapEnumValue attributes in the syntax tree
                    var enumAttributeSyntaxes = fieldDeclaration.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(a => a.Name.ToString() == "MapEnumValue" && a.ArgumentList?.Arguments.Count == 2)
                        .ToList();

                    // Match semantic attributes with syntax attributes based on their argument values
                    for (int i = 0; i < enumMappingAttributes.Count && i < enumAttributeSyntaxes.Count; i++)
                    {
                        var attr = enumMappingAttributes[i];
                        var sourceValueName = attr.ConstructorArguments[0].Value as string;
                        var targetValueName = attr.ConstructorArguments[1].Value as string;

                        // Find the corresponding attribute syntax
                        var matchingSyntax = enumAttributeSyntaxes.FirstOrDefault(a =>
                            GetArgumentStringValue(a.ArgumentList!.Arguments[0], semanticModel) == sourceValueName &&
                            GetArgumentStringValue(a.ArgumentList!.Arguments[1], semanticModel) == targetValueName);

                        var attrSyntax = matchingSyntax ?? enumAttributeSyntaxes[i];

                        mappingInfo.EnumMappings.Add(
                            new MappingMemberInfo(sourceValueName, targetValueName, attrSyntax));
                    }

                    // Find any custom property mappings
                    var propertyMappingAttributes = fieldSymbol.GetAttributes()
                        .Where(a => a.AttributeClass?.Name == "MapPropertyAttribute")
                        .ToList();

                    // Find all MapProperty attributes in the syntax tree
                    var propAttributeSyntaxes = fieldDeclaration.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(a => a.Name.ToString() == "MapProperty" && a.ArgumentList?.Arguments.Count == 2)
                        .ToList();

                    // Match semantic attributes with syntax attributes based on their argument values
                    for (int i = 0; i < propertyMappingAttributes.Count && i < propAttributeSyntaxes.Count; i++)
                    {
                        var attr = propertyMappingAttributes[i];
                        var sourcePropertyName = attr.ConstructorArguments[0].Value as string;
                        var targetPropertyName = attr.ConstructorArguments[1].Value as string;

                        // Find the corresponding attribute syntax
                        var matchingSyntax = propAttributeSyntaxes.FirstOrDefault(a =>
                            GetArgumentStringValue(a.ArgumentList!.Arguments[0], semanticModel) == sourcePropertyName &&
                            GetArgumentStringValue(a.ArgumentList!.Arguments[1], semanticModel) == targetPropertyName);

                        var attrSyntax = matchingSyntax ?? propAttributeSyntaxes[i];

                        mappingInfo.PropertyMappings.Add(
                            new MappingMemberInfo(sourcePropertyName, targetPropertyName, attrSyntax));
                    }

                    mapperClass.Mappings.Add(mappingInfo);
                }
            }

            return mapperClass.Mappings.Count > 0 ? mapperClass : null;
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

        private static void GenerateMapper(SourceProductionContext context, MapperClassInfo? mapperClass)
        {
            if (mapperClass == null)
            {
                return;
            }

            // Validate mappings
            foreach (var mapping in mapperClass.Mappings)
            {
                // Validate enum mappings
                foreach (var enumMapping in mapping.EnumMappings)
                {
                    ValidateEnumMapping(context, mapping, enumMapping);
                }

                // Validate property mappings
                foreach (var propertyMapping in mapping.PropertyMappings)
                {
                    ValidatePropertyMapping(context, mapping, propertyMapping);
                }
            }

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

        private static void ValidateEnumMapping(SourceProductionContext context, MappingTypeInfo mapping, MappingMemberInfo enumMapping)
        {
            bool sourceValueExists = false;
            bool targetValueExists = false;

            // Find all enum types in the source type
            foreach (var member in mapping.SourceType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.Type.TypeKind == TypeKind.Enum))
            {
                var enumType = member.Type as INamedTypeSymbol;
                if (enumType != null)
                {
                    foreach (var enumMember in enumType.GetMembers().OfType<IFieldSymbol>()
                        .Where(f => f.IsStatic && f.IsConst || f.IsReadOnly))
                    {
                        if (enumMember.Name == enumMapping.SourceName)
                        {
                            sourceValueExists = true;
                            break;
                        }
                    }
                }

                if (sourceValueExists)
                    break;
            }

            // Find all enum types in the destination type
            foreach (var member in mapping.DestinationType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.Type.TypeKind == TypeKind.Enum))
            {
                var enumType = member.Type as INamedTypeSymbol;
                if (enumType != null)
                {
                    foreach (var enumMember in enumType.GetMembers().OfType<IFieldSymbol>()
                        .Where(f => f.IsStatic && f.IsConst || f.IsReadOnly))
                    {
                        if (enumMember.Name == enumMapping.TargetName)
                        {
                            targetValueExists = true;
                            break;
                        }
                    }
                }

                if (targetValueExists)
                    break;
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

        private static IPropertySymbol FindMatchingSourceProperty(MappingTypeInfo mapping, IParameterSymbol parameter)
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