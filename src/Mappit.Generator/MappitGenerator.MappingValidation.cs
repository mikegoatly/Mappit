using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    public partial class MappitGenerator : IIncrementalGenerator
    {
        private static ValidatedMapperClassInfo ValidateMappings(SourceProductionContext context, MapperClassInfo mapperClass)
        {
            var validatedMapperClass = new ValidatedMapperClassInfo(mapperClass);

            foreach (var mapping in mapperClass.Mappings)
            {
                if (mapping.ValidationError != MappingTypeValidationError.None)
                {
                    ReportDiagnostic(
                        context,
                        $"Mapping error for '{mapping.SourceType}' to '{mapping.TargetType}': {mapping.ValidationError}",
                        mapping.MethodDeclaration);

                    continue;
                }

                if (mapping.IsEnum)
                {
                    ValidateEnumMapping(context, mapping, validatedMapperClass);
                }
                else
                {
                    ValidateTypeMapping(context, mapperClass, mapping, validatedMapperClass);
                }
            }

            return validatedMapperClass;
        }

        private static void ValidateEnumMapping(SourceProductionContext context, MappingTypeInfo mapping, ValidatedMapperClassInfo validatedMapperClassInfo)
        {
            var validatedMapping = new ValidatedMappingEnumInfo(mapping);

            var sourceMembers = mapping.SourceType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.IsStatic && f.IsConst || f.IsReadOnly).ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var targetMembers = mapping.TargetType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.IsStatic && f.IsConst || f.IsReadOnly).ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            // First validate any custom mappings that have been provided
            foreach (var enumMapping in mapping.MemberMappings.Values)
            {
                if (!sourceMembers.TryGetValue(enumMapping.SourceName, out var sourceMember))
                {
                    ReportDiagnostic(context,
                        $"Source enum value '{enumMapping.SourceName}' not found in any enum property of type '{mapping.SourceType.Name}'",
                        enumMapping.SourceArgument);
                }

                if (!targetMembers.TryGetValue(enumMapping.TargetName, out var targetMember))
                {
                    ReportDiagnostic(context,
                        $"Target enum value '{enumMapping.TargetName}' not found in any enum property of type '{mapping.TargetType.Name}'",
                        enumMapping.TargetArgument);
                }

                validatedMapping.MemberMappings.Add(new ValidatedMappingEnumMemberInfo(sourceMember, targetMember));
            }

            ValidateRemainingEnumMembers(context, mapping, validatedMapping, sourceMembers, targetMembers);

            validatedMapperClassInfo.EnumMappings.Add(validatedMapping);
        }

        private static void ValidateTypeMapping(SourceProductionContext context, MapperClassInfo mapperClass, MappingTypeInfo mapping, ValidatedMapperClassInfo validatedMapperClassInfo)
        {
            var validatedMapping = new ValidatedMappingTypeInfo(mapping);

            var sourceProperties = mapping.SourceType.GetMembers().OfType<IPropertySymbol>()
                .Where(f => !f.IsStatic && !f.IsWriteOnly && !f.IsImplicitlyDeclared)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var targetProperties = mapping.TargetType.GetMembers().OfType<IPropertySymbol>()
                .Where(f => !f.IsStatic  && !f.IsImplicitlyDeclared)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            // First validate any custom mappings that have been provided
            foreach (var propertyMapping in mapping.MemberMappings.Values)
            {
                // Report diagnostics if properties don't exist
                if (!sourceProperties.TryGetValue(propertyMapping.SourceName, out var sourceProperty))
                {
                    ReportDiagnostic(context,
                        $"Source property '{propertyMapping.SourceName}' not found in type '{mapping.SourceType.Name}'",
                        propertyMapping.SourceArgument);
                }

                if (!targetProperties.TryGetValue(propertyMapping.TargetName, out var targetProperty))
                {
                    ReportDiagnostic(context,
                        $"Target property '{propertyMapping.TargetName}' not found in type '{mapping.TargetType.Name}'",
                        propertyMapping.TargetArgument);
                }

                // Check if property types are compatible
                if (sourceProperty != null && targetProperty != null)
                {
                    bool isCompatible = AreCompatibleTypes(mapperClass, sourceProperty.Type, targetProperty.Type);
                    if (!isCompatible)
                    {
                        ReportDiagnostic(context,
                            $"Incompatible types for property mapping: '{sourceProperty.Type.Name}' to '{targetProperty.Type.Name}'",
                            propertyMapping.SyntaxNode);
                    }
                    else
                    {
                        validatedMapping.MemberMappings[targetProperty.Name] = new ValidatedMappingMemberInfo(sourceProperty, targetProperty);
                    }
                }
            }

            ValidateRemainingPropertyMappings(context, mapperClass, mapping, validatedMapping, sourceProperties, targetProperties);

            if (ValidateConstructionRequirements(context, validatedMapping))
            {
                validatedMapperClassInfo.TypeMappings.Add(validatedMapping);
            }
        }

        private static bool ValidateConstructionRequirements(SourceProductionContext context, ValidatedMappingTypeInfo mapping)
        {
            var bestCtor = FindBestConstructor(mapping);

            if (bestCtor is null)
            {
                ReportDiagnostic(context,
                    $"No suitable constructor found for type '{mapping.TargetType.Name}'. Parameter names must match the target type's property names.",
                    mapping.MethodDeclaration);

                return false;
            }

            // Now we have the best match we can work out which properties are mapped via the constructor, and which must be initialized.
            // Then we can validate that the target properties aren't read only.
            var constructorParams = bestCtor.Parameters.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var propertyMapping in mapping.MemberMappings.Values)
            {
                if (constructorParams.TryGetValue(propertyMapping.TargetProperty.Name, out var constructorParam))
                {
                    propertyMapping.MappingKind = PropertyMappingKind.Constructor;

                    // TODO validate type is the same as the target property type
                }
                else
                {
                    // This property is not mapped via the constructor, so we need to check if it's read only
                    if (propertyMapping.TargetProperty.IsReadOnly)
                    {
                        ReportDiagnostic(
                            context,
                            $"Target property '{propertyMapping.TargetProperty.Name}' is read only and cannot be set.",
                            mapping.MethodDeclaration);

                        return false;
                    }

                    propertyMapping.MappingKind = PropertyMappingKind.Initialization;
                }
            }

            mapping.Constructor = bestCtor;

            return true;
        }

        private static IMethodSymbol? FindBestConstructor(ValidatedMappingTypeInfo mapping)
        {
            (IMethodSymbol? ctor, int bestMatchCount) bestCtor = (null, 0);

            var ctors = mapping.TargetType.GetMembers()
                .Where(m => m.Kind == SymbolKind.Method && m.Name == ".ctor")
                .Cast<IMethodSymbol>()
                .OrderByDescending(m => m.Parameters.Length)
                .ToList();

            foreach (var ctor in ctors)
            {
                int matchingParams = 0;

                foreach (var param in ctor.Parameters)
                {
                    // Try to find a mapping that matches parameter name and type. The lookup is keyed by
                    // the target property name, so we're expecting the target constructor parameter name to
                    // match the target property name.
                    if (mapping.MemberMappings.ContainsKey(param.Name))
                    {
                        matchingParams++;
                    }
                    else
                    {
                        // There's a parameter we can't satisfy, so we can't use this constructor
                        // Using -1 is a bit of a hack, but it causes the rest of the checks after
                        // this to fail, so we end up not considering it.
                        matchingParams = -1;
                        break;
                    }
                }

                // If all parameters can be matched, this is a good constructor choice
                if (matchingParams == ctor.Parameters.Length)
                {
                    bestCtor = (ctor, matchingParams);
                    break;
                }

                // Update best match if this constructor has more matching parameters
                if (matchingParams > bestCtor.bestMatchCount)
                {
                    bestCtor = (ctor, matchingParams);
                }
            }

            return bestCtor.ctor;
        }

        private static bool AreCompatibleTypes(MapperClassInfo mapperClass, ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            // Simple case: if the types are the same, they're compatible
            if (sourceType.Equals(targetType, SymbolEqualityComparer.Default))
            {
                return true;
            }

            // Check if the types are compatible because they've been mapped by the user.
            // For example, sourceType may be TypeA and targetType may be TypeB, which are not the same type, but they
            // may be compatible because the user has mapped them.
            return mapperClass.Mappings.Any(m =>
                m.SourceType.Equals(sourceType, SymbolEqualityComparer.Default) &&
                m.TargetType.Equals(targetType, SymbolEqualityComparer.Default));
        }

        private static void ValidateRemainingPropertyMappings(
            SourceProductionContext context,
            MapperClassInfo mapperClass,
            MappingTypeInfo mappingInfo,
            ValidatedMappingTypeInfo validatedMapping,
            Dictionary<string, IPropertySymbol> sourceProperties,
            Dictionary<string, IPropertySymbol> targetProperties)
        {
            foreach (var sourceMember in sourceProperties.Values)
            {
                // Only check for properties that are not already mapped
                if (!mappingInfo.MemberMappings.ContainsKey(sourceMember.Name))
                {
                    // Do we have a matching target property?
                    if (!targetProperties.TryGetValue(sourceMember.Name, out var targetMember))
                    {
                        // If we're not ignoring missing properties, report a diagnostic
                        if (!mappingInfo.IgnoreMissingPropertiesOnTarget)
                        {
                            ReportDiagnostic(context,
                                $"Property '{sourceMember.Name}' not found in target type '{mappingInfo.TargetType.Name}'. " +
                                $"Use [{nameof(IgnoreMissingPropertiesOnTargetAttribute)}] to ignore this error.",
                                mappingInfo.MethodDeclaration);
                        }
                        // Otherwise just skip this property
                    }
                    else
                    {
                        // Check if property types are compatible
                        if (!AreCompatibleTypes(mapperClass, sourceMember.Type, targetMember.Type))
                        {
                            ReportDiagnostic(context,
                                $"Incompatible types for property mapping: '{sourceMember.Type.Name}' to '{targetMember.Type.Name}'",
                                mappingInfo.MethodDeclaration);
                        }
                        else
                        {
                            validatedMapping.MemberMappings[targetMember.Name] = new ValidatedMappingMemberInfo(sourceMember, targetMember);
                        }
                    }
                }
            }
        }

        private static void ValidateRemainingEnumMembers(
            SourceProductionContext context,
            MappingTypeInfo mappingInfo,
            ValidatedMappingEnumInfo validatedMapping,
            Dictionary<string, IFieldSymbol> sourceMembers,
            Dictionary<string, IFieldSymbol> targetMembers)
        {
            foreach (var sourceMember in sourceMembers.Values)
            {
                // Only check for values that are not already mapped
                if (!mappingInfo.MemberMappings.ContainsKey(sourceMember.Name))
                {
                    // Do we have a matching target property?
                    if (!targetMembers.TryGetValue(sourceMember.Name, out var targetMember))
                    {
                        ReportDiagnostic(context,
                            $"Target enum value '{sourceMember.Name}' not found in type '{mappingInfo.TargetType.Name}'. ",
                            mappingInfo.MethodDeclaration);
                    }
                    else
                    {
                        validatedMapping.MemberMappings.Add(new ValidatedMappingEnumMemberInfo(sourceMember, targetMember));
                    }
                }
            }
        }

        // Helper method to check if a property is compiler-generated
        private static bool IsCompilerGenerated(IPropertySymbol property)
        {
            // Check if the property itself has the CompilerGeneratedAttribute
            var hasCompilerGeneratedAttribute = property.GetAttributes()
                .Any(attr => attr.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

            return hasCompilerGeneratedAttribute;
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
