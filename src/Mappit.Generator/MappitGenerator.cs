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
            if (mapperClass is null)
            {
                return;
            }

            var validatedMap = ValidateMappings(context, mapperClass);

            var source = new StringBuilder();

            // Generate namespace start
            source.AppendLine($"namespace {validatedMap.Namespace}");
            source.AppendLine("{");

            // Generate partial class implementation
            source.AppendLine($"    public partial class {validatedMap.ClassName}");
            source.AppendLine("    {");

            // Generate constructor that initializes all mappings
            source.AppendLine($"        // Auto-generated constructor for {validatedMap.ClassName}");
            source.AppendLine($"        public {validatedMap.ClassName}()");
            source.AppendLine("        {");
            source.AppendLine("            // Initialize all mappings");

            // Generate mapping implementations and initialization
            foreach (var mapping in validatedMap.EnumMappings.Concat<ValidatedMappingInfo>(validatedMap.TypeMappings))
            {
                // Initialize and register each mapping
                source.AppendLine($"            {mapping.FieldName} = new {mapping.FieldName}Mapping();");
                source.AppendLine($"            RegisterMapping({mapping.FieldName});");
            }

            source.AppendLine("        }");

            // Generate mapping class implementations for each type mapping declaration
            foreach (var mapping in validatedMap.TypeMappings)
            {
                GenerateTypeMappingClass(source, validatedMap, mapping);
            }

            // Generate mapping class implementations for each enum mapping declaration
            foreach (var mapping in validatedMap.EnumMappings)
            {
                GenerateEnumMappingClass(source, mapping);
            }

            source.AppendLine("    }");
            source.AppendLine("}");

            // Add the source code to the compilation
            context.AddSource($"{validatedMap.ClassName}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }

        private static void EmitSourcePropertyReference(StringBuilder source, ValidatedMapperClassInfo classInfo, ValidatedMappingMemberInfo member)
        {
            if (classInfo.IsMappedType(member.SourceProperty.Type, member.TargetProperty.Type))
            {
                source.Append($"mapper.Map<{member.TargetProperty.Type.Name}>(typedSource.{member.SourceProperty.Name})");
            }
            else
            {
                source.Append($"typedSource.{member.SourceProperty.Name}");
            }
        }

        private static void GenerateTypeMappingClass(StringBuilder source, ValidatedMapperClassInfo classInfo, ValidatedMappingTypeInfo mapping)
        {
            var sourceTypeName = mapping.SourceType.Name;
            var destTypeName = mapping.DestinationType.Name;
            var fieldName = mapping.FieldName;

            source.AppendLine();
            source.AppendLine($"        // Implement {fieldName} mapping from {sourceTypeName} to {destTypeName}");
            source.AppendLine($"        private sealed class {fieldName}Mapping : Mappit.TypeMapping<{sourceTypeName}, {destTypeName}>");
            source.AppendLine("        {");
            source.AppendLine($"            public override {destTypeName} Map(IMapper mapper, {sourceTypeName} typedSource)");
            source.AppendLine("            {");

            // Start object initialization
            source.AppendLine($"                return new {destTypeName}");

            if (mapping.Constructor.Parameters.Length > 0)
            {
                // Generate constructor arguments
                source.Append("                (");

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
                source.AppendLine("                ()");
            }

            if (mapping.MemberMappings.Values.Any(m => m.MappingKind == PropertyMappingKind.Initialization))
            {
                // Start object initializer
                source.AppendLine("                {");

                // Handle custom property mappings first (skip those already set by constructor)
                foreach (var propertyMapping in mapping.MemberMappings.Values.Where(x => x.MappingKind == PropertyMappingKind.Initialization))
                {
                    source.Append($"                    {propertyMapping.TargetProperty.Name} = ");
                    EmitSourcePropertyReference(source, classInfo, propertyMapping);
                    source.AppendLine(",");
                }

                // Remove the last comma and newline
                source.Length -= 2;
                source.AppendLine();

                // Close the object initializer
                source.Append("                }");
            }

            // Add the semicolon after the initializer or constructor.
            source.AppendLine(";");

            source.AppendLine("            }");
            source.AppendLine("        }");
        }

        private static void GenerateEnumMappingClass(StringBuilder source, ValidatedMappingEnumInfo mapping)
        {
            var sourceTypeName = mapping.SourceType.Name;
            var destTypeName = mapping.DestinationType.Name;
            var fieldName = mapping.FieldName;

            source.AppendLine();
            source.AppendLine($"        // Implement {fieldName} mapping from {sourceTypeName} to {destTypeName}");
            source.AppendLine($"        private sealed class {fieldName}Mapping : Mappit.TypeMapping<{sourceTypeName}, {destTypeName}>");
            source.AppendLine("        {");
            source.AppendLine($"            public override {destTypeName} Map(IMapper mapper, {sourceTypeName} source)");
            source.AppendLine("            {");
            source.AppendLine($"                return source switch");
            source.AppendLine("                {");

            // Generate enum case mappings
            foreach (var enumCase in mapping.MemberMappings)
            {
                source.AppendLine($"                    {sourceTypeName}.{enumCase.SourceField.Name} => {destTypeName}.{enumCase.TargetField.Name},");
            }

            // Add a default case to handle unmapped values
            source.AppendLine($"                    _ => throw new ArgumentOutOfRangeException(nameof(source), $\"Invalid enum value {{source}}\")");
            source.AppendLine("                };");
            source.AppendLine("            }");
            source.AppendLine("        }");
        }
    }
}