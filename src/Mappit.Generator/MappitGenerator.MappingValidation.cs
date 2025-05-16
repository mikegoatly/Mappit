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
            ValidateClassSyntax(context, mapperClass);

            var validatedMapperClass = new ValidatedMapperClassInfo(mapperClass);

            foreach (var mapping in mapperClass.Mappings)
            {
                if (mapping.ValidationErrors.Count > 0)
                {
                    foreach (var (code, message) in mapping.ValidationErrors)
                    {
                        context.ReportDiagnostic(code, message, mapping.MethodDeclaration);
                    }

                    continue;
                }

                if (mapping.IsEnum)
                {
                    ValidateEnumMapping(context, mapperClass, mapping, validatedMapperClass);
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

        private static void ValidateClassSyntax(SourceProductionContext context, MapperClassInfo mapperClass)
        {
            var modifiers = mapperClass.ClassDeclarationSyntax.Modifiers;
            if (!modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                context.ReportDiagnostic(
                    MappitErrorCode.MapperClassNotPartial,
                    $"Mapper class '{mapperClass.ClassDeclarationSyntax.Identifier.Text}' must be partial.",
                    mapperClass.ClassDeclarationSyntax);
            }
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
                context.ReportDiagnostic(
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
                context.ReportDiagnostic(
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

        private static void ValidateEnumMapping(SourceProductionContext context, MapperClassInfo mapperClass, MappingTypeInfo mapping, ValidatedMapperClassInfo validatedMapperClass)
        {
            // Handle nullable enums
            var sourceType = mapping.SourceType;
            var targetType = mapping.TargetType;
            var sourceIsNullable = sourceType.IsNullableType();
            var targetIsNullable = targetType.IsNullableType();
            
            // Get the actual enum types (unwrap nullable if needed)
            var sourceEnumType = sourceType.GetNullableUnderlyingType();
            var targetEnumType = targetType.GetNullableUnderlyingType();

            var sourceMembers = sourceEnumType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.IsStatic && f.IsConst || f.IsReadOnly).ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var targetMembers = targetEnumType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.IsStatic && f.IsConst || f.IsReadOnly).ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            if (mapping.PropertyMappings.Count > 0)
            {
                context.ReportDiagnostic(
                    MappitErrorCode.EnumMappingWithPropertyMappings,
                    $"Enum mapping '{mapping.MethodName}' cannot have property mappings.",
                    mapping.MethodDeclaration);
            }

            var memberMappings = new List<ValidatedMappingEnumMemberInfo>();

            // First validate any custom mappings that have been provided
            foreach (var enumMapping in mapping.EnumValueMappings.Values)
            {
                if (!sourceMembers.TryGetValue(enumMapping.SourceName, out var sourceMember))
                {
                    context.ReportDiagnostic(
                        MappitErrorCode.UserMappedSourceEnumValueNotFound,
                        $"Source enum value '{enumMapping.SourceName}' not found in any enum property of type '{FormatTypeForErrorMessage(sourceEnumType)}'",
                        enumMapping.SourceArgument);
                }

                if (!targetMembers.TryGetValue(enumMapping.TargetName, out var targetMember))
                {
                    context.ReportDiagnostic(
                        MappitErrorCode.UserMappedTargetEnumValueNotFound,
                        $"Target enum value '{enumMapping.TargetName}' not found in any enum property of type '{FormatTypeForErrorMessage(targetEnumType)}'",
                        enumMapping.TargetArgument);
                }

                if (sourceMember is not null && targetMember is not null)
                {
                    memberMappings.Add(new ValidatedMappingEnumMemberInfo(sourceMember, targetMember));
                }
            }

            ValidateRemainingEnumMembers(context, mapping, memberMappings, sourceMembers, targetMembers);

            if (sourceIsNullable || targetIsNullable)
            {
                // Is there already a mapping for the underlying enum, either user defined, or currently calculated?
                // TODO consider carrying around the original user mapper class with the validated mapper class so we can unify these checks and reduce passed parameters
                if (!(mapperClass.HasHapping(sourceEnumType, targetEnumType) || validatedMapperClass.HasHapping(sourceEnumType, targetEnumType)))
                {
                    // Create an additional mapping for the nullable versions of the type
                    validatedMapperClass.EnumMappings.Add(ValidatedMappingEnumInfo.Implicit(mapping, sourceEnumType, targetEnumType, memberMappings));
                }
                
                // Also register the explicit nullable mapping for the type
                validatedMapperClass.NullableMappings.Add(ValidatedNullableMappingTypeInfo.Explicit(mapping));
            }
            else
            {
                // Register the validated enum mapping
                validatedMapperClass.EnumMappings.Add(ValidatedMappingEnumInfo.Explicit(mapping, memberMappings));
            }
        }

        private static bool ValidateTypeMapping(SourceProductionContext context, MapperClassInfo mapperClass, MappingTypeInfo mapping, ValidatedMapperClassInfo validatedMapperClass)
        {
            if (mapping.EnumValueMappings.Count > 0)
            {
                context.ReportDiagnostic(
                    MappitErrorCode.TypeMappingWithEnumValueMappings,
                    $"Type mapping '{mapping.MethodName}' cannot have enum value mappings.",
                    mapping.MethodDeclaration);
            }

            var sourceIsNullable = mapping.SourceType.IsNullableType();
            var targetIsNullable = mapping.TargetType.IsNullableType();
            var sourceType = mapping.SourceType.GetNullableUnderlyingType();
            var targetType = mapping.TargetType.GetNullableUnderlyingType();

            // We only consider source properties that are:
            // * Publicly accessible
            // * Not static
            // * Not write-only (i.e. they have a getter)
            var sourceProperties = GetMappableProperties(sourceType)
                .Where(f => !f.IsWriteOnly)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var targetProperties = GetMappableProperties(targetType)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            // First validate any custom mappings that have been provided
            var successfullyValidated = true;
            var memberMappings = new ValidatedMappingMemberInfoSet();

            foreach (var propertyMapping in mapping.PropertyMappings.Values)
            {
                // Report diagnostics if properties don't exist
                if (!sourceProperties.TryGetValue(propertyMapping.SourceName, out var sourceProperty))
                {
                    context.ReportDiagnostic(
                        MappitErrorCode.UserMappedSourcePropertyNotFound,
                        $"Source property '{propertyMapping.SourceName}' not found in type '{FormatTypeForErrorMessage(sourceType)}'",
                        propertyMapping.SourceArgument);

                    successfullyValidated = false;
                }

                if (!targetProperties.TryGetValue(propertyMapping.TargetName, out var targetProperty))
                {
                    context.ReportDiagnostic(
                        MappitErrorCode.UserMappedTargetPropertyNotFound,
                        $"Target property '{propertyMapping.TargetName}' not found in type '{FormatTypeForErrorMessage(targetType)}'",
                        propertyMapping.TargetArgument);

                    successfullyValidated = false;
                }

                if (sourceProperty != null && targetProperty != null)
                {
                    // Check if property types are compatible
                    bool isCompatible = propertyMapping.ValueConversionMethod is not null || AreCompatibleTypes(mapperClass, sourceProperty.Type, targetProperty.Type);
                    if (!isCompatible)
                    {
                        memberMappings.Add(ValidatedMappingMemberInfo.Invalid(sourceProperty, targetProperty));
                        ReportIncompatibleSourceAndTargetPropertyTypesDiagnostic(context, sourceProperty, targetProperty, propertyMapping.SyntaxNode);
                        successfullyValidated = false;
                    }
                    else
                    {
                        if (propertyMapping.ValueConversionMethod is { } conversionMethod)
                        {
                            if (ValidateValueConversionMethod(context, propertyMapping.SyntaxNode, sourceProperty.Type, targetProperty.Type, conversionMethod))
                            {
                                memberMappings.Add(ValidatedMappingMemberInfo.Valid(sourceProperty, targetProperty, conversionMethod));
                            }
                            else
                            {
                                memberMappings.Add(ValidatedMappingMemberInfo.Invalid(sourceProperty, targetProperty));
                            }
                        }
                        else
                        {
                            memberMappings.Add(ValidatedMappingMemberInfo.Valid(sourceProperty, targetProperty));
                        }
                    }
                }
            }

            ValidateRemainingPropertyMappings(context, mapperClass, validatedMapperClass, mapping, memberMappings, sourceProperties, targetProperties);

            if (ValidateConstructionRequirements(context, mapperClass, mapping, targetType, memberMappings, out var constructor))
            {
                if (sourceIsNullable || targetIsNullable)
                {
                    // Is there already a mapping for the underlying enum, either user defined, or currently calculated?
                    // TODO consider carrying around the original user mapper class with the validated mapper class so we can unify these checks and reduce passed parameters
                    if (!(mapperClass.HasHapping(sourceType, targetType) || validatedMapperClass.HasHapping(sourceType, targetType)))
                    {
                        // Create an additional mapping for the nullable versions of the type
                        validatedMapperClass.TypeMappings.Add(ValidatedMappingTypeInfo.Implicit(mapping, sourceType, targetType, memberMappings, constructor!));
                    }

                    // Also register the explicit nullable mapping for the type
                    validatedMapperClass.NullableMappings.Add(ValidatedNullableMappingTypeInfo.Explicit(mapping));
                }
                else
                {
                    // Register the validated enum mapping
                    validatedMapperClass.TypeMappings.Add(ValidatedMappingTypeInfo.Explicit(mapping, memberMappings, constructor!));
                }

                return successfullyValidated;
            }

            return false;
        }

        /// <summary>
        /// Validates that the value conversion has the return type of the target property
        /// and has a single parameter with the type of the source property.
        /// </summary>
        private static bool ValidateValueConversionMethod(
            SourceProductionContext context,
            SyntaxNode mappingSyntaxNode,
            ITypeSymbol sourceType,
            ITypeSymbol targetType,
            IMethodSymbol valueConversionMethod)
        {
            // Check the return type of the value conversion method matches the target property type
            if (!valueConversionMethod.ReturnType.Equals(targetType, SymbolEqualityComparer.Default))
            {
                context.ReportDiagnostic(
                    MappitErrorCode.InvalidValueConversionReturnType,
                    $"Value conversion method '{valueConversionMethod.Name}' return type '{FormatTypeForErrorMessage(valueConversionMethod.ReturnType)}' does not match target property type '{FormatTypeForErrorMessage(targetType)}'",
                    mappingSyntaxNode);

                return false;
            }

            // Check the parameter type of the value conversion method matches the source property type
            if (valueConversionMethod.Parameters.Length != 1 || !valueConversionMethod.Parameters[0].Type.Equals(sourceType, SymbolEqualityComparer.Default))
            {
                context.ReportDiagnostic(
                    MappitErrorCode.InvalidValueConversionParameterType,
                    $"Value conversion method '{valueConversionMethod.Name}' parameter type '{FormatTypeForErrorMessage(valueConversionMethod.Parameters[0].Type)}' does not match source property type '{FormatTypeForErrorMessage(sourceType)}'",
                    mappingSyntaxNode);

                return false;
            }

            return true;
        }

        private static void ReportIncompatibleSourceAndTargetPropertyTypesDiagnostic(
            SourceProductionContext context,
            IPropertySymbol sourceMember,
            IPropertySymbol targetMember,
            SyntaxNode syntaxNode)
        {
            context.ReportDiagnostic(
                MappitErrorCode.IncompatibleSourceAndTargetPropertyTypes,
                $"Incompatible types for property mapping: {sourceMember.Name} ({FormatTypeForErrorMessage(sourceMember.Type)}) to {targetMember.Name} ({FormatTypeForErrorMessage(targetMember.Type)})",
                syntaxNode);
        }

        private static bool ValidateConstructionRequirements(
            SourceProductionContext context, 
            MapperClassInfo mapperClass, 
            MappingTypeInfo mapping,
            ITypeSymbol targetType, 
            ValidatedMappingMemberInfoSet memberMappings,
            out IMethodSymbol? constructor)
        {
            constructor = default;
            var bestCtor = FindBestConstructor(targetType, memberMappings);

            if (bestCtor is null)
            {
                context.ReportDiagnostic(
                    MappitErrorCode.NoSuitableConstructorFound,
                    $"No suitable constructor found for type '{FormatTypeForErrorMessage(targetType)}'. Parameter names must match the target type's property names.",
                    mapping.MethodDeclaration);

                return false;
            }

            // Now we have the best match we can work out which properties are mapped via the constructor, and which must be initialized.
            // Then we can validate that the target properties aren't read only.
            var constructorParams = bestCtor.Parameters.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var propertyMapping in memberMappings)
            {
                if (constructorParams.TryGetValue(propertyMapping.TargetProperty.Name, out var constructorParam))
                {
                    propertyMapping.TargetMapping = TargetMapping.Constructor;

                    if (!AreCompatibleTypes(mapperClass, propertyMapping.SourceProperty.Type, constructorParam.Type))
                    {
                        context.ReportDiagnostic(
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
                        context.ReportDiagnostic(
                            MappitErrorCode.TargetPropertyReadOnly,
                            $"Target property '{propertyMapping.TargetProperty.Name}' is read only and cannot be set.",
                            mapping.MethodDeclaration);

                        return false;
                    }

                    propertyMapping.TargetMapping = TargetMapping.Initialization;
                }
            }

            constructor = bestCtor;
            return true;
        }

        private static IMethodSymbol? FindBestConstructor(ITypeSymbol targetType, ValidatedMappingMemberInfoSet memberMappings)
        {
            (IMethodSymbol? ctor, int bestMatchCount) bestCtor = (null, 0);

            var ctors = targetType.GetMembers()
                .Where(m => m.Kind == SymbolKind.Method && m.Name == ".ctor")
                .Cast<IMethodSymbol>()
                .OrderByDescending(m => m.Parameters.Length)
                .ToList();

            foreach (var ctor in ctors)
            {
                int matchingParams = 0;

                foreach (var param in ctor.Parameters)
                {
                    // Try to find a mapping that matches parameter name and type. We're expecting the
                    // target constructor parameter name to match the target property name.
                    if (memberMappings.ContainsTargetName(param.Name))
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
            ValidatedMappingMemberInfoSet memberMappings,
            Dictionary<string, IPropertySymbol> sourceProperties,
            Dictionary<string, IPropertySymbol> targetProperties)
        {
            foreach (var sourceMember in sourceProperties.Values)
            {
                // Only check for properties that are not already mapped
                if (!mappingInfo.PropertyMappings.ContainsKey(sourceMember.Name))
                {
                    // Do we have a matching target property?
                    if (!targetProperties.TryGetValue(sourceMember.Name, out var targetMember))
                    {
                        // If we're not ignoring missing properties, report a diagnostic
                        if (!mappingInfo.IgnoreMissingPropertiesOnTarget)
                        {
                            context.ReportDiagnostic(
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
                            memberMappings.Add(ValidatedMappingMemberInfo.Invalid(sourceMember, targetMember));
                            ReportIncompatibleSourceAndTargetPropertyTypesDiagnostic(context, sourceMember, targetMember, mappingInfo.MethodDeclaration);
                        }
                        else
                        {
                            // The mapping is valid, so we can add it to the validated mapping
                            memberMappings.Add(ValidatedMappingMemberInfo.Valid(sourceMember, targetMember));

                            // If the mapped property is a collection of some sort, we also need to generate some additional
                            // type mappings that implement the collection mapping logic.
                            ConfigureImplicitCollectionMappings(validatedMapperClass, mappingInfo.MethodDeclaration, sourceMember.Type, targetMember.Type);

                            // If the mapped source or target property is a nullable type, we also need to add maps for them
                            ConfigureImplicitNullableTypeMappings(mapperClass, validatedMapperClass, mappingInfo.MethodDeclaration, sourceMember.Type, targetMember.Type);
                        }
                    }
                }
            }
        }

        private static void ConfigureImplicitNullableTypeMappings(
            MapperClassInfo mapperClass,
            ValidatedMapperClassInfo validatedMapperClass,
            SyntaxNode methodDeclaration,
            ITypeSymbol sourceType, 
            ITypeSymbol targetType)
        {
            var sourceIsNullable = sourceType.IsNullableType();
            var targetIsNullable = targetType.IsNullableType();

            if (sourceIsNullable || targetIsNullable)
            {
                // Is there already a direct mapping configured between the two?
                if (validatedMapperClass.HasHapping(sourceType, targetType))
                {
                    // Nothing to do here, we already have a mapping for this type
                    return;
                }

                // We don't need to emit an implicit conversion if *both* types are nullable and no
                // conversion is required between the two, e.g. DateTime? to DateTime?
                // By the time we get here, we will have already checked for compatibility between
                // the underlying types, so if there is no explicit map, then we can assume that the
                // types are compatible.
                if (sourceIsNullable 
                    && targetIsNullable 
                    && !mapperClass.HasHapping(sourceType.GetNullableUnderlyingType(), targetType.GetNullableUnderlyingType()))
                {
                    return;
                }

                // TODO If the target is not nullable, but the source is, raise a warning
                // that there may be conversion errors at runtime.

                // Create an additional mapping for the nullable versions of the type
                validatedMapperClass.NullableMappings.Add(
                    ValidatedNullableMappingTypeInfo.Implicit(sourceType, targetType, methodDeclaration));
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
            List<ValidatedMappingEnumMemberInfo> memberMappings,
            Dictionary<string, IFieldSymbol> sourceMembers,
            Dictionary<string, IFieldSymbol> targetMembers)
        {
            foreach (var sourceMember in sourceMembers.Values)
            {
                // Only check for values that are not already mapped
                if (!mappingInfo.EnumValueMappings.ContainsKey(sourceMember.Name))
                {
                    // Do we have a matching target property?
                    if (!targetMembers.TryGetValue(sourceMember.Name, out var targetMember))
                    {
                        context.ReportDiagnostic(
                            MappitErrorCode.ImplicitTargetEnumValueNotFound,
                            $"Target enum value '{sourceMember.Name}' not found in type '{FormatTypeForErrorMessage(mappingInfo.TargetType)}'. ",
                            mappingInfo.MethodDeclaration);
                    }
                    else
                    {
                        memberMappings.Add(new ValidatedMappingEnumMemberInfo(sourceMember, targetMember));
                    }
                }
            }
        }

        private static bool AreCompatibleTypes(MapperClassInfoBase mapperClass, ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            return TypeCompatibilityChecker.AreCompatibleTypes(mapperClass, sourceType, targetType);
        }

        /// <summary>
        /// Gets all properties from a type, including inherited properties.
        /// </summary>
        private static IEnumerable<IPropertySymbol> GetMappableProperties(ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                foreach (var member in current.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.DeclaredAccessibility == Accessibility.Public && !member.IsStatic)
                    {
                        yield return member;
                    }
                }
                
                current = current.BaseType;
            }
        }

        private static string FormatTypeForErrorMessage(ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        }
    }
}
