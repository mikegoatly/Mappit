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
            if (classInfo.TryGetMappedType(member.SourceProperty.Type, member.TargetProperty.Type, out var mapping))
            {
                source.Append($"{mapping!.MethodName}(source.{member.SourceProperty.Name})");
            }
            else
            {
                source.Append($"source.{member.SourceProperty.Name}");
            }
        }

        private static void GenerateTypeMappingMethod(StringBuilder source, ValidatedMapperClassInfo classInfo, ValidatedMappingTypeInfo mapping)
        {
            if (!mapping.RequiresGeneration)
            {
                return;
            }

            var sourceTypeName = mapping.SourceType.Name;
            var targetTypeName = mapping.TargetType.Name;

            source.AppendLine();
            source.AppendLine($"        // Implementation of mapping from {sourceTypeName} to {targetTypeName}");
            source.AppendLine($"        public partial {targetTypeName} {mapping.MethodName}({sourceTypeName} source)");
            source.AppendLine("        {");
            source.AppendLine("            if (source == null)");
            source.AppendLine("            {");
            source.AppendLine("                return default;");
            source.AppendLine("            }");
            source.AppendLine();

            // Start object initialization
            source.AppendLine($"            return new {targetTypeName}");

            if (mapping.Constructor.Parameters.Length > 0)
            {
                // Generate constructor arguments
                source.Append("            (");

                for (int i = 0; i < mapping.Constructor.Parameters.Length; i++)
                {
                    var param = mapping.Constructor.Parameters[i];

                    EmitSourcePropertyReference(source, classInfo, mapping.MemberMappings[param.Name]);

                    if (i < mapping.Constructor.Parameters.Length - 1)
                    {
                        source.Append(", ");
                    }
                }

                source.AppendLine(")");
            }
            else
            {
                // Default constructor; no args
                source.AppendLine("            ()");
            }

            if (mapping.MemberMappings.Values.Any(m => m.MappingKind == PropertyMappingKind.Initialization))
            {
                // Start object initializer
                source.AppendLine("            {");

                // Handle custom property mappings first (skip those already set by constructor)
                foreach (var propertyMapping in mapping.MemberMappings.Values.Where(x => x.MappingKind == PropertyMappingKind.Initialization))
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

            var sourceTypeName = mapping.SourceType.Name;
            var targetTypeName = mapping.TargetType.Name;

            source.AppendLine();
            source.AppendLine($"        // Implementation of mapping from {sourceTypeName} to {targetTypeName}");
            source.AppendLine($"        public partial {targetTypeName} {mapping.MethodName}({sourceTypeName} source)");
            source.AppendLine("        {");
            source.AppendLine($"            return source switch");
            source.AppendLine("            {");

            // Generate enum case mappings
            foreach (var enumCase in mapping.MemberMappings)
            {
                source.AppendLine($"                {sourceTypeName}.{enumCase.SourceField.Name} => {targetTypeName}.{enumCase.TargetField.Name},");
            }

            // Add a default case to handle unmapped values
            source.AppendLine($"                _ => throw new global::System.ArgumentOutOfRangeException(nameof(source), $\"Invalid enum value {{source}}\")");
            source.AppendLine("            };");
            source.AppendLine("        }");
        }
    }
}