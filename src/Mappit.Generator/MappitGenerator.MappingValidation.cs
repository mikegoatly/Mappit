using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mappit.Generator
{
    public partial class MappitGenerator : IIncrementalGenerator
    {
        private static ValidatedMapperClassInfo ValidateMappings(SourceProductionContext context, MapperClassInfo mapperClass)
        {
            // Validate the mapper class for the basics
            if (!mapperClass.ClassDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                ReportDiagnostic(
                    context,
                    MappitErrorCode.MapperClassNotPartial,
                    $"Mapper class '{mapperClass.ClassDeclarationSyntax.Identifier.Text}' must be partial.",
                    mapperClass.ClassDeclarationSyntax);
            }

            var validatedMapperClass = new ValidatedMapperClassInfo(mapperClass);

            foreach (var mapping in mapperClass.Mappings)
            {
                if (mapping.ValidationError != MappitErrorCode.None)
                {
                    ReportDiagnostic(
                        context,
                        mapping.ValidationError,
                        $"Mapping error for '{FormatTypeForErrorMessage(mapping.SourceType)}' to '{FormatTypeForErrorMessage(mapping.TargetType)}'",
                        mapping.MethodDeclaration);

                    continue;
                }

                if (mapping.IsEnum)
                {
                    ValidateEnumMapping(context, mapping, validatedMapperClass);
                }
                else if (TypeHelpers.IsDictionaryType(mapping.SourceType, out var sourceKeyType, out var sourceElementType))
                { 
                    ValidateDictionaryTypeMapping(context, mapperClass, mapping, validatedMapperClass, sourceKeyType!, sourceElementType!);
                }
                else if (TypeHelpers.IsCollectionType(mapping.SourceType, out sourceElementType))
                {
                    ValidateCollectionTypeMapping(context, mapperClass, mapping, validatedMapperClass, sourceElementType!);
                }
                else
                {
                    ValidateTypeMapping(context, mapperClass, mapping, validatedMapperClass);
                }
            }

            return validatedMapperClass;
        }

        private static void ValidateCollectionTypeMapping(
            SourceProductionContext context,
            MapperClassInfo mapperClass,
            MappingTypeInfo mapping, 
            ValidatedMapperClassInfo validatedMapperClass, 
            ITypeSymbol sourceElementType)
        {
            if (!TypeHelpers.IsCollectionType(mapping.TargetType, out var targetElementType))
            {
                ReportDiagnostic(
                    context,
                    MappitErrorCode.InvalidCollectionTypeMapping,
                    $"Invalid dictionary type mapping: {FormatTypeForErrorMessage(mapping.SourceType)} to {FormatTypeForErrorMessage(mapping.TargetType)}",
                    mapping.MethodDeclaration);

                return;
            }

            // Register the validated collection mapping
            validatedMapperClass.CollectionMappings.Add(
                ValidatedCollectionMappingTypeInfo.Explicit(mapping, CollectionKind.Collection, (sourceElementType, targetElementType!)));

            ValidateExplicitlyMappedCollectionGenericType(context, mapperClass, mapping, validatedMapperClass, sourceElementType, targetElementType!);
        }

        private static void ValidateDictionaryTypeMapping(
            SourceProductionContext context,
            MapperClassInfo mapperClass,
            MappingTypeInfo mapping, 
            ValidatedMapperClassInfo validatedMapperClass, 
            ITypeSymbol sourceKeyType, 
            ITypeSymbol sourceElementType)
        {
            if (!TypeHelpers.IsDictionaryType(mapping.TargetType, out var targetKeyType, out var targetElementType))
            {
                ReportDiagnostic(
                    context,
                    MappitErrorCode.InvalidDictionaryTypeMapping,
                    $"Invalid dictionary type mapping: {FormatTypeForErrorMessage(mapping.SourceType)} to {FormatTypeForErrorMessage(mapping.TargetType)}",
                    mapping.MethodDeclaration);

                return;
            }

            // Register the validated dictionary mapping
            validatedMapperClass.CollectionMappings.Add(
                ValidatedCollectionMappingTypeInfo.Explicit(mapping, CollectionKind.Dictionary, (sourceElementType, targetElementType!), (sourceKeyType, targetKeyType!)));

            ValidateExplicitlyMappedCollectionGenericType(context, mapperClass, mapping, validatedMapperClass, sourceElementType, targetElementType!);
            ValidateExplicitlyMappedCollectionGenericType(context, mapperClass, mapping, validatedMapperClass, sourceKeyType, targetKeyType!);
        }

        private static bool ValidateExplicitlyMappedCollectionGenericType(
            SourceProductionContext context,
            MapperClassInfo mapperClass,
            MappingTypeInfo mapping,
            ValidatedMapperClassInfo validatedMapperClass,
            ITypeSymbol sourceType,
            ITypeSymbol targetType)
        {
            // Do we need to map the element type at all?
            if (AreCompatibleTypes(mapperClass, sourceType, targetType!))
            {
                // Nothing more to do for this collection
                return false;
            }

            // Is the element type explicitly registered by the user?
            // We need to check both the original user mappings and any validated mappings that have been created
            // because we can't guarantee that the validated mappings contains all the user mappings yet, but it
            // *may* contain additional mappings that the user hasn't defined.
            if (mapperClass.HasHapping(sourceType, targetType!)
                || validatedMapperClass.HasHapping(sourceType, targetType!))
            {
                // TODO Emit a warning that it's unnecessary to register both
            }
            else
            {
                // We need to also register the type mapping between the element types
                var elementMappingTypeInfo = mapping with
                {
                    SourceType = sourceType,
                    TargetType = targetType!,

                    // This mapping method hasn't been defined by the user, so it won't require a partial method
                    RequiresPartialMethod = false,
                };

                ValidateTypeMapping(context, mapperClass, elementMappingTypeInfo, validatedMapperClass);
            }

            return true;
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
                    ReportDiagnostic(
                        context,
                        MappitErrorCode.UserMappedSourceEnumValueNotFound,
                        $"Source enum value '{enumMapping.SourceName}' not found in any enum property of type '{FormatTypeForErrorMessage(mapping.SourceType)}'",
                        enumMapping.SourceArgument);
                }

                if (!targetMembers.TryGetValue(enumMapping.TargetName, out var targetMember))
                {
                    ReportDiagnostic(
                        context,
                        MappitErrorCode.UserMappedTargetEnumValueNotFound,
                        $"Target enum value '{enumMapping.TargetName}' not found in any enum property of type '{FormatTypeForErrorMessage(mapping.TargetType)}'",
                        enumMapping.TargetArgument);
                }

                validatedMapping.MemberMappings.Add(new ValidatedMappingEnumMemberInfo(sourceMember, targetMember));
            }

            ValidateRemainingEnumMembers(context, mapping, validatedMapping, sourceMembers, targetMembers);

            validatedMapperClassInfo.EnumMappings.Add(validatedMapping);
        }

        private static bool ValidateTypeMapping(SourceProductionContext context, MapperClassInfo mapperClass, MappingTypeInfo mapping, ValidatedMapperClassInfo validatedMapperClass)
        {
            var validatedMapping = new ValidatedMappingTypeInfo(mapping);

            // We only consider source properties that are:
            // * Publicly accessible
            // * Not static
            // * Not write-only (i.e. they have a getter)
            var sourceProperties = mapping.SourceType.GetMembers().OfType<IPropertySymbol>()
                .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic && !f.IsWriteOnly)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var targetProperties = mapping.TargetType.GetMembers().OfType<IPropertySymbol>()
                .Where(f => f.DeclaredAccessibility == Accessibility.Public && !f.IsStatic)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            // First validate any custom mappings that have been provided
            var successfullyValidated = true;
            foreach (var propertyMapping in mapping.MemberMappings.Values)
            {
                // Report diagnostics if properties don't exist
                if (!sourceProperties.TryGetValue(propertyMapping.SourceName, out var sourceProperty))
                {
                    ReportDiagnostic(
                        context,
                        MappitErrorCode.UserMappedSourcePropertyNotFound,
                        $"Source property '{propertyMapping.SourceName}' not found in type '{FormatTypeForErrorMessage(mapping.SourceType)}'",
                        propertyMapping.SourceArgument);

                    successfullyValidated = false;
                }

                if (!targetProperties.TryGetValue(propertyMapping.TargetName, out var targetProperty))
                {
                    ReportDiagnostic(
                        context,
                        MappitErrorCode.UserMappedTargetPropertyNotFound,
                        $"Target property '{propertyMapping.TargetName}' not found in type '{FormatTypeForErrorMessage(mapping.TargetType)}'",
                        propertyMapping.TargetArgument);

                    successfullyValidated = false;
                }

                if (sourceProperty != null && targetProperty != null)
                {
                    // Check if property types are compatible
                    bool isCompatible = AreCompatibleTypes(mapperClass, sourceProperty.Type, targetProperty.Type);
                    if (!isCompatible)
                    {
                        validatedMapping.MemberMappings[targetProperty.Name] = ValidatedMappingMemberInfo.Invalid(sourceProperty, targetProperty);
                        ReportIncompatibleSourceAndTargetPropertyTypesDiagnostic(context, sourceProperty, targetProperty, propertyMapping.SyntaxNode);
                        successfullyValidated = false;
                    }
                    else
                    {
                        validatedMapping.MemberMappings[targetProperty.Name] = ValidatedMappingMemberInfo.Valid(sourceProperty, targetProperty);
                    }
                }
            }

            ValidateRemainingPropertyMappings(context, mapperClass, validatedMapperClass, mapping, validatedMapping, sourceProperties, targetProperties);

            if (ValidateConstructionRequirements(context, mapperClass, validatedMapping))
            {
                validatedMapperClass.TypeMappings.Add(validatedMapping);
                return successfullyValidated;
            }

            return false;
        }

        private static void ReportIncompatibleSourceAndTargetPropertyTypesDiagnostic(
            SourceProductionContext context,
            IPropertySymbol sourceMember,
            IPropertySymbol targetMember,
            SyntaxNode syntaxNode)
        {
            ReportDiagnostic(
                context,
                MappitErrorCode.IncompatibleSourceAndTargetPropertyTypes,
                $"Incompatible types for property mapping: {sourceMember.Name} ({FormatTypeForErrorMessage(sourceMember.Type)}) to {targetMember.Name} ({FormatTypeForErrorMessage(targetMember.Type)})",
                syntaxNode);
        }

        private static bool ValidateConstructionRequirements(SourceProductionContext context, MapperClassInfo mapperClass, ValidatedMappingTypeInfo mapping)
        {
            var bestCtor = FindBestConstructor(mapping);

            if (bestCtor is null)
            {
                ReportDiagnostic(
                    context,
                    MappitErrorCode.NoSuitableConstructorFound,
                    $"No suitable constructor found for type '{FormatTypeForErrorMessage(mapping.TargetType)}'. Parameter names must match the target type's property names.",
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
                    propertyMapping.TargetMapping = TargetMapping.Constructor;

                    if (!AreCompatibleTypes(mapperClass, propertyMapping.SourceProperty.Type, constructorParam.Type))
                    {
                        ReportDiagnostic(
                            context,
                            MappitErrorCode.IncompatibleSourceAndConstructorPropertyTypes,
                            $"Incompatible types for constructor mapping: {propertyMapping.SourceProperty.Name} " +
                                $"({FormatTypeForErrorMessage(propertyMapping.SourceProperty.Type)}) to parameter " +
                                $"{constructorParam.Name} ({FormatTypeForErrorMessage(constructorParam.Type)})",
                            mapping.MethodDeclaration);

                        return false;
                    }
                }
                else
                {
                    // This property is not mapped via the constructor, so we need to check if it's read only
                    if (propertyMapping.TargetProperty.IsReadOnly)
                    {
                        ReportDiagnostic(
                            context,
                            MappitErrorCode.TargetPropertyReadOnly,
                            $"Target property '{propertyMapping.TargetProperty.Name}' is read only and cannot be set.",
                            mapping.MethodDeclaration);

                        return false;
                    }

                    propertyMapping.TargetMapping = TargetMapping.Initialization;
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

        private static void ValidateRemainingPropertyMappings(
            SourceProductionContext context,
            MapperClassInfo mapperClass,
            ValidatedMapperClassInfo validatedMapperClass,
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
                            validatedMapping.MemberMappings[targetMember.Name] = ValidatedMappingMemberInfo.Invalid(sourceMember, targetMember);

                            ReportDiagnostic(
                                context,
                                MappitErrorCode.ImplicitMappedTargetPropertyNotFound,
                                $"Property '{sourceMember.Name}' not found in target type '{FormatTypeForErrorMessage(mappingInfo.TargetType)}'. " +
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
                            validatedMapping.MemberMappings[targetMember.Name] = ValidatedMappingMemberInfo.Invalid(sourceMember, targetMember);
                            ReportIncompatibleSourceAndTargetPropertyTypesDiagnostic(context, sourceMember, targetMember, mappingInfo.MethodDeclaration);
                        }
                        else
                        {
                            // The mapping is valid, so we can add it to the validated mapping
                            validatedMapping.MemberMappings[targetMember.Name] = ValidatedMappingMemberInfo.Valid(sourceMember, targetMember);

                            // If the mapped property is a collection of some sort, we also need to generate some additional
                            // type mappings that implement the collection mapping logic.
                            ConfigureImplicitCollectionMappings(validatedMapperClass, mappingInfo.MethodDeclaration, sourceMember.Type, targetMember.Type);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Configures implicit mappings required for collections/dictionaries.
        /// </summary>
        private static void ConfigureImplicitCollectionMappings(
            ValidatedMapperClassInfo mapperClass, 
            SyntaxNode originatingSyntaxNode,
            ITypeSymbol sourceType, 
            ITypeSymbol targetType)
        {
            // Check if both properties are dictionaries
            if (TypeHelpers.IsDictionaryType(sourceType, out var sourceKeyType, out var sourceValueType) &&
                 TypeHelpers.IsDictionaryType(targetType, out var targetKeyType, out var targetValueType))
            {
                if (sourceKeyType != null && targetKeyType != null && sourceValueType != null && targetValueType != null)
                {
                    // Check we don't already have a mapping for the dictionary type - if not create one.
                    if (!mapperClass.HasHapping(sourceType, targetType))
                    {
                        // We've not got a mapping for this exact source to target type yet
                        mapperClass.CollectionMappings.Add(
                            ValidatedCollectionMappingTypeInfo.Implicit(
                                sourceType, 
                                targetType, 
                                originatingSyntaxNode, 
                                CollectionKind.Dictionary,
                                (sourceValueType, targetValueType),
                                (sourceKeyType, targetKeyType)));
                    }

                    ConfigureImplicitCollectionMappings(mapperClass, originatingSyntaxNode, sourceKeyType, targetKeyType);
                    ConfigureImplicitCollectionMappings(mapperClass, originatingSyntaxNode, sourceValueType, targetValueType);
                }
            }
            // Check if both properties are collections
            else if (TypeHelpers.IsCollectionType(sourceType, out var sourceElementType) &&
                TypeHelpers.IsCollectionType(targetType, out var targetElementType))
            {
                if (sourceElementType != null && targetElementType != null)
                {
                    // Check we don't already have a mapping for the collection type - if not create one.
                    if (!mapperClass.HasHapping(sourceType, targetType))
                    {
                        // We've not got a mapping for this exact source to target type yet
                        mapperClass.CollectionMappings.Add(
                            ValidatedCollectionMappingTypeInfo.Implicit(
                                sourceType, 
                                targetType, 
                                originatingSyntaxNode, 
                                CollectionKind.Collection,
                                (sourceElementType, targetElementType)));
                    }

                    ConfigureImplicitCollectionMappings(mapperClass, originatingSyntaxNode, sourceElementType, targetElementType);
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
                        ReportDiagnostic(
                            context,
                            MappitErrorCode.ImplicitTargetEnumValueNotFound,
                            $"Target enum value '{sourceMember.Name}' not found in type '{FormatTypeForErrorMessage(mappingInfo.TargetType)}'. ",
                            mappingInfo.MethodDeclaration);
                    }
                    else
                    {
                        validatedMapping.MemberMappings.Add(new ValidatedMappingEnumMemberInfo(sourceMember, targetMember));
                    }
                }
            }
        }

        private static bool AreCompatibleTypes(MapperClassInfoBase mapperClass, ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            // Simple case: if the types are the same, they're compatible
            if (sourceType.Equals(targetType, SymbolEqualityComparer.Default))
            {
                return true;
            }

            // Check if the types are compatible because they've already been mapped.
            // For example, sourceType may be TypeA and targetType may be TypeB, which are not the same type, but they
            // may be compatible because the user has mapped them.
            if (mapperClass.HasHapping(sourceType, targetType))
            {
                return true;
            }

            // Dictionary types - these also have collection/enumerable interfaces, so we need to check them first
            if (TypeHelpers.IsDictionaryType(sourceType, out var sourceKeyType, out var sourceValueType) &&
                TypeHelpers.IsDictionaryType(targetType, out var targetKeyType, out var targetValueType))
            {
                if (sourceKeyType != null && targetKeyType != null && sourceValueType != null && targetValueType != null)
                {
                    // Dictionaries are compatible if their key and value types are compatible
                    return AreCompatibleTypes(mapperClass, sourceKeyType, targetKeyType) &&
                           AreCompatibleTypes(mapperClass, sourceValueType, targetValueType);
                }
            }

            // Check for collection types
            if (TypeHelpers.IsCollectionType(sourceType, out var sourceElementType) &&
                TypeHelpers.IsCollectionType(targetType, out var targetElementType))
            {
                if (sourceElementType != null && targetElementType != null)
                {
                    // Collections are compatible if their element types are compatible
                    return AreCompatibleTypes(mapperClass, sourceElementType, targetElementType);
                }
            }

            return false;
        }

        private static void ReportDiagnostic(
            SourceProductionContext context,
            MappitErrorCode errorCode,
            string message,
            SyntaxNode node)
        {
            var (id, title) = ErrorCodes.GetError(errorCode);
            var descriptor = new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: message,
                category: "Mappit",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            var location = Location.Create(node.SyntaxTree, node.Span);
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
        }


        private static string FormatTypeForErrorMessage(ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        }
    }
}
