using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mappit.Generator
{

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
                    // Transform to get mapper class info
                    transform: static (ctx, _) => GetMapperClassInfo(ctx))
                .Where(static m => m != null);

            // Register the output generator
            context.RegisterSourceOutput(classDeclarations, GenerateMapper);
        }

        private static void GenerateMapper(SourceProductionContext context, MapperClassInfo? mapperClass)
        {
            if (mapperClass is null)
            {
                return;
            }

            var validatedMap = ValidateMappings(context, mapperClass);

            var source = new StringBuilder();

            // Generate namespace start
            source.AppendLine($"namespace {validatedMap.Namespace}");
            source.AppendLine("{");

            // Generate interface first
            GenerateMapperInterface(source, validatedMap);

            // Generate partial class implementation
            source.AppendLine($"    public partial class {validatedMap.ClassName} : I{validatedMap.ClassName}");
            source.AppendLine("    {");

            // Implementation for each mapping method
            foreach (var mapping in validatedMap.EnumMappings)
            {
                GenerateEnumMappingMethod(source, mapping);
            }

            foreach (var mapping in validatedMap.TypeMappings)
            {
                GenerateTypeMappingMethod(source, validatedMap, mapping);
            }

            foreach (var mapping in validatedMap.ImplicitCollectionMappings)
            {
                if (mapping.CollectionKind == CollectionKind.Collection)
                {
                    EmitCollectionMappingMethod(source, validatedMap, mapping);
                }
                else
                {
                    EmitDictionaryMappingMethod(source, validatedMap, mapping);
                }
            }

            source.AppendLine("    }");
            source.AppendLine("}");

            // Add the source code to the compilation
            context.AddSource($"{validatedMap.ClassName}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }

        private static void GenerateMapperInterface(StringBuilder source, ValidatedMapperClassInfo validatedMap)
        {
            source.AppendLine($"    /// <summary>");
            source.AppendLine($"    /// Interface for {validatedMap.ClassName} class");
            source.AppendLine($"    /// </summary>");
            source.AppendLine($"    public interface I{validatedMap.ClassName}");
            source.AppendLine("    {");

            // Generate interface methods for each mapping type
            foreach (var mapping in validatedMap.EnumMappings)
            {
                source.AppendLine($"        /// <summary>");
                source.AppendLine($"        /// Maps {mapping.SourceType.Name} to {mapping.TargetType.Name}");
                source.AppendLine($"        /// </summary>");
                source.AppendLine($"        {mapping.TargetType.Name} {mapping.MethodName}({mapping.SourceType.Name} source);");
                source.AppendLine();
            }

            foreach (var mapping in validatedMap.TypeMappings)
            {
                source.AppendLine($"        /// <summary>");
                source.AppendLine($"        /// Maps {mapping.SourceType.Name} to {mapping.TargetType.Name}");
                source.AppendLine($"        /// </summary>");
                source.AppendLine($"        {mapping.TargetType.Name} {mapping.MethodName}({mapping.SourceType.Name} source);");
                source.AppendLine();
            }

            source.AppendLine("    }");
            source.AppendLine();
        }

        private static void EmitSourcePropertyReference(StringBuilder source, ValidatedMapperClassInfo classInfo, ValidatedMappingMemberInfo member)
        {
            if (!member.IsValid)
            {
                // When we haven't been able to resolve the mapping, we need to emit a placeholder.
                // We've already emitted an error message, so we can just emit a comment here.
                // Doing this means we limit the number of additional compiler errors reported in the generated code.
                source.AppendLine("// TODO: Unable to resolve mapping for {member.SourceProperty.Name} to {member.TargetProperty.Name}");
                source.Append($"                    default");
            }
            else
            {
                // Check if there is a direct mapping from source to target property type
                if (classInfo.TryGetMappedType(member.SourceProperty.Type, member.TargetProperty.Type, out var mapping))
                {
                    source.Append($"{mapping!.MethodName}(source.{member.SourceProperty.Name})");
                }
                else
                {
                    source.Append($"source.{member.SourceProperty.Name}");
                }
            }
        }

        private static void EmitCollectionMappingMethod(StringBuilder source, ValidatedMapperClassInfo classInfo, ValidatedCollectionMappingTypeInfo mapping)
        {
            var (sourceElementType, targetElementType) = mapping.ElementTypeMap;

            EmitMappingMethodDeclaration(source, mapping);
            source.AppendLine("            if (source is null)");
            source.AppendLine("            {");
            source.AppendLine("                return default;");
            source.AppendLine("            }");
            source.AppendLine();
            source.Append("            ");

            bool needsElementMapping = classInfo.TryGetMappedType(sourceElementType, targetElementType, out var elementMapping);
            if (needsElementMapping)
            {
                // We'll use LINQ to enumerate the source collection and map each element
                // Handle arrays specially
                if (mapping.TargetType.TypeKind == TypeKind.Array)
                {
                    source.AppendLine($"return source.Select({elementMapping!.MethodName}).ToArray();");
                }
                else
                {
                    // For other collections, create a new instance of the appropriate type
                    // Use the concrete type that was determined during validation
                    var concreteReturnType = TypeHelpers.InferConcreteCollectionType(mapping.TargetType, targetElementType);
                    source.AppendLine($"return new {concreteReturnType}(source.Select({elementMapping!.MethodName}));");
                }
            }
            else
            {
                // We don't need to use LINQ for the mapping; we can just construct the new collection type from the
                // source collection directly
                // TODO - this could be optimised further if we allow the user to opt in to re-using the source collection
                if (mapping.TargetType.TypeKind == TypeKind.Array)
                {
                    source.AppendLine($"return source.ToArray();");
                }
                else
                {
                    var concreteReturnType = TypeHelpers.InferConcreteCollectionType(mapping.TargetType, targetElementType);
                    source.AppendLine($"return new {concreteReturnType}(source);");
                }
            }

            source.AppendLine("        }");
        }

        private static void EmitDictionaryMappingMethod(StringBuilder source, ValidatedMapperClassInfo classInfo, ValidatedCollectionMappingTypeInfo mapping)
        {
            // Check if there are mappings for key and value types
            var (sourceKeyType, targetKeyType) = mapping.KeyTypeMap
                ?? throw new InvalidOperationException("KeyTypeMap not set for dictionary mapping");
            bool needsKeyMapping = classInfo.TryGetMappedType(sourceKeyType, targetKeyType, out var keyMapping);

            var (sourceElementType, targetElementType) = mapping.ElementTypeMap;
            bool needsValueMapping = classInfo.TryGetMappedType(sourceElementType, targetElementType, out var valueMapping);

            EmitMappingMethodDeclaration(source, mapping);
            source.AppendLine("            if (source is null)");
            source.AppendLine("            {");
            source.AppendLine("                return default;");
            source.AppendLine("            }");
            source.AppendLine();
            source.Append("            ");

            var concreteReturnType = TypeHelpers.InferConcreteDictionaryType(mapping.TargetType, targetKeyType, targetElementType);
            if (needsKeyMapping || needsValueMapping)
            {
                // Create new dictionary of the concrete type that was determined during validation
                source.AppendLine($"return new {concreteReturnType}(");
                source.Append($"                source.Select(kvp => ");
                source.Append("new global::System.Collections.Generic.KeyValuePair<");
                source.Append(targetKeyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                source.Append(", ");
                source.Append(targetElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                source.Append(">(");

                // Key mapping
                source.Append(needsKeyMapping ? $"{keyMapping!.MethodName}(kvp.Key)" : "kvp.Key");
                source.Append(", ");

                // Value mapping
                source.Append(needsValueMapping ? $"{valueMapping!.MethodName}(kvp.Value)" : "kvp.Value");

                source.Append(")));");
            }
            else
            {
                // We'll just create a new dictionary of the appropriate type
                // Use the concrete type that was determined during validation
                // TODO - this could be optimised further if we allow the user to opt in to re-using the source collection
                source.Append($"return new {concreteReturnType}(source);");
            }

            source.AppendLine("        }");
        }

        private static void GenerateTypeMappingMethod(StringBuilder source, ValidatedMapperClassInfo classInfo, ValidatedMappingTypeInfo mapping)
        {
            if (!mapping.RequiresGeneration)
            {
                return;
            }

            EmitMappingMethodDeclaration(source, mapping);
            source.AppendLine("            if (source is null)");
            source.AppendLine("            {");
            source.AppendLine("                return default;");
            source.AppendLine("            }");
            source.AppendLine();

            // Start object initialization
            source.Append($"            return new {mapping.TargetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");

            var ctor = mapping.Constructor
                ?? throw new InvalidOperationException($"Constructor not set!");

            if (ctor.Parameters.Length > 0)
            {
                // Generate constructor arguments
                source.AppendLine("(");

                for (int i = 0; i < ctor.Parameters.Length; i++)
                {
                    var param = ctor.Parameters[i];

                    source.Append("                ");
                    EmitSourcePropertyReference(source, classInfo, mapping.MemberMappings[param.Name]);

                    if (i < ctor.Parameters.Length - 1)
                    {
                        source.AppendLine(", ");
                    }
                }

                source.Append(")");
            }
            else
            {
                // Default constructor; no args
                source.Append("()");
            }

            if (mapping.MemberMappings.Values.Any(m => m.TargetMapping == TargetMapping.Initialization))
            {
                // Start object initializer
                source.AppendLine();
                source.AppendLine("            {");

                // Handle custom property mappings first (skip those already set by constructor)
                foreach (var propertyMapping in mapping.MemberMappings.Values.Where(x => x.TargetMapping == TargetMapping.Initialization))
                {
                    source.Append($"                {propertyMapping.TargetProperty.Name} = ");
                    EmitSourcePropertyReference(source, classInfo, propertyMapping);
                    source.AppendLine(",");
                }

                // Remove the last comma and newline
                source.Length -= 2;
                source.AppendLine();

                // Close the object initializer
                source.Append("            }");
            }

            // Add the semicolon after the initializer or constructor.
            source.AppendLine(";");

            source.AppendLine("        }");
        }

        private static void GenerateEnumMappingMethod(StringBuilder source, ValidatedMappingEnumInfo mapping)
        {
            if (!mapping.RequiresGeneration)
            {
                return;
            }

            EmitMappingMethodDeclaration(source, mapping);

            source.AppendLine($"            return source switch");
            source.AppendLine("            {");

            // Generate enum case mappings
            var sourceTypeName = mapping.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var targetTypeName = mapping.TargetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            foreach (var enumCase in mapping.MemberMappings)
            {
                source.AppendLine($"                {sourceTypeName}.{enumCase.SourceField.Name} => {targetTypeName}.{enumCase.TargetField.Name},");
            }

            // Add a default case to handle unmapped values
            source.AppendLine($"                _ => throw new global::System.ArgumentOutOfRangeException(nameof(source), $\"Invalid enum value {{source}}\")");
            source.AppendLine("            };");
            source.AppendLine("        }");
        }

        private static void EmitMappingMethodDeclaration(StringBuilder source, ValidatedMappingInfo mapping)
        {
            var fullyQualifiedTargetName = mapping.TargetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var fullyQualifiedSourceName = mapping.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            source.AppendLine();
            source.AppendLine($"        // Implementation of mapping from {mapping.SourceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
            source.AppendLine($"        // to {mapping.TargetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
            source.AppendLine($"        public {(mapping.RequiresPartialMethod ? "partial " : "")}{fullyQualifiedTargetName} {mapping.MethodName}({fullyQualifiedSourceName} source)");
            source.AppendLine("        {");
        }
    }
}