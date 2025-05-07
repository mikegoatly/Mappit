using System;
using System.Collections.Generic;
using System.Linq;

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

        private static void ValidateTypeMapping(SourceProductionContext context, MapperClassInfo mapperClass, MappingTypeInfo mapping, ValidatedMapperClassInfo validatedMapperClassInfo)
        {
            var validatedMapping = new ValidatedMappingTypeInfo(mapping);

            var sourceProperties = mapping.SourceType.GetMembers().OfType<IPropertySymbol>()
                .Where(f => !f.IsStatic && !f.IsWriteOnly && !f.IsImplicitlyDeclared)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var targetProperties = mapping.TargetType.GetMembers().OfType<IPropertySymbol>()
                .Where(f => !f.IsStatic && !f.IsImplicitlyDeclared)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            // First validate any custom mappings that have been provided
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
                }

                if (!targetProperties.TryGetValue(propertyMapping.TargetName, out var targetProperty))
                {
                    ReportDiagnostic(
                        context,
                        MappitErrorCode.UserMappedTargetPropertyNotFound,
                        $"Target property '{propertyMapping.TargetName}' not found in type '{FormatTypeForErrorMessage(mapping.TargetType)}'",
                        propertyMapping.TargetArgument);
                }

                if (sourceProperty != null && targetProperty != null)
                {
                    // Check if property types are compatible
                    bool isCompatible = AreCompatibleTypes(mapperClass, sourceProperty.Type, targetProperty.Type);
                    if (!isCompatible)
                    {
                        ReportIncompatibleSourceAndTargetPropertyTypesDiagnostic(context, sourceProperty, targetProperty, propertyMapping.SyntaxNode);
                    }
                    else
                    {
                        validatedMapping.MemberMappings[targetProperty.Name] = new ValidatedMappingMemberInfo(sourceProperty, targetProperty);
                    }
                }
            }

            ValidateRemainingPropertyMappings(context, mapperClass, mapping, validatedMapping, sourceProperties, targetProperties);

            if (ValidateConstructionRequirements(context, mapperClass, validatedMapping))
            {
                validatedMapperClassInfo.TypeMappings.Add(validatedMapping);
            }
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
                    propertyMapping.TargetMapping = TargetMappingKind.Constructor;

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

                    propertyMapping.TargetMapping = TargetMappingKind.Initialization;
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
                            ReportIncompatibleSourceAndTargetPropertyTypesDiagnostic(context, sourceMember, targetMember, mappingInfo.MethodDeclaration);
                        }
                        else
                        {
                            var memberInfo = new ValidatedMappingMemberInfo(sourceMember, targetMember);
                            ConfigureCollectionMappingInfo(mapperClass, sourceMember, targetMember, memberInfo);
                            validatedMapping.MemberMappings[targetMember.Name] = memberInfo;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Configures collection mapping information if the properties represent collections or dictionaries
        /// </summary>
        private static void ConfigureCollectionMappingInfo(
            MapperClassInfo mapperClass,
            IPropertySymbol sourceProperty, 
            IPropertySymbol targetProperty, 
            ValidatedMappingMemberInfo memberInfo)
        {
            //if (memberInfo.SourceProperty.Name == "AdditionalIEnumerableInterface")
            //{
           //    System.Diagnostics.Debugger.Launch();
            //}

            // Check if both properties are collections
            if (TypeHelpers.IsCollectionType(sourceProperty.Type, out var sourceElementType) &&
                TypeHelpers.IsCollectionType(targetProperty.Type, out var targetElementType))
            {
                if (sourceElementType != null && targetElementType != null)
                {
                    memberInfo.PropertyMappingKind = PropertyKind.Collection;
                    memberInfo.ElementTypeMap = (sourceElementType, targetElementType);
                    
                    // Infer the concrete collection type based on the target property interface
                    memberInfo.ConcreteTargetType = TypeHelpers.InferConcreteCollectionType(targetProperty.Type, targetElementType);
                }
            }
            // Check if both properties are dictionaries
            else if (TypeHelpers.IsDictionaryType(sourceProperty.Type, out var sourceKeyType, out var sourceValueType) &&
                     TypeHelpers.IsDictionaryType(targetProperty.Type, out var targetKeyType, out var targetValueType))
            {
                if (sourceKeyType != null && targetKeyType != null && sourceValueType != null && targetValueType != null)
                {
                    memberInfo.PropertyMappingKind = PropertyKind.Dictionary;
                    memberInfo.KeyTypeMap = (sourceKeyType, targetKeyType);
                    memberInfo.ElementTypeMap = (sourceValueType, targetValueType);
                    
                    // Infer the concrete dictionary type based on the target property interface
                    memberInfo.ConcreteTargetType = TypeHelpers.InferConcreteDictionaryType(targetProperty.Type, targetKeyType, targetValueType);
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
            if (mapperClass.Mappings.Any(m =>
                m.SourceType.Equals(sourceType, SymbolEqualityComparer.Default) &&
                m.TargetType.Equals(targetType, SymbolEqualityComparer.Default)))
            {
                return true;
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

            // Check for dictionary types
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
