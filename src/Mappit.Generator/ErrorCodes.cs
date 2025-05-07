using System;
using System.Collections.Generic;
using System.Text;

namespace Mappit.Generator
{
    public enum MappitErrorCode
    {
        None,
        UserMappedTargetEnumValueNotFound,
        ImplicitTargetEnumValueNotFound,
        UserMappedSourceEnumValueNotFound,
        ImplicitMappedTargetPropertyNotFound,
        UserMappedTargetPropertyNotFound,
        UserMappedSourcePropertyNotFound,
        IncompatibleSourceAndTargetPropertyTypes,
        TargetPropertyReadOnly,
        NoSuitableConstructorFound,
        IncompatibleSourceAndConstructorPropertyTypes,
        EnumTypeMismatch,
        CannotReverseMapCustomMapping,
    }

    public static class ErrorCodes
    {
        internal static (string id, string title) GetError(MappitErrorCode errorCode)
        {
            return errorCode switch
            {
                MappitErrorCode.UserMappedTargetEnumValueNotFound => 
                    ("MAPPIT001", "User mapped target enum value not found"),
                MappitErrorCode.ImplicitTargetEnumValueNotFound => 
                    ("MAPPIT002", "Implicitly mapped target enum value not found"),
                MappitErrorCode.UserMappedSourceEnumValueNotFound => 
                    ("MAPPIT003", "User mapped source enum value not found"),
                MappitErrorCode.ImplicitMappedTargetPropertyNotFound => 
                    ("MAPPIT004", "Implicitly mapped target property not found"),
                MappitErrorCode.UserMappedTargetPropertyNotFound => 
                    ("MAPPIT005", "User mapped target property not found"),
                MappitErrorCode.UserMappedSourcePropertyNotFound => 
                    ("MAPPIT006", "User mapped source property not found"),
                MappitErrorCode.IncompatibleSourceAndTargetPropertyTypes => 
                    ("MAPPIT007", "Incompatible source and target property types"),
                MappitErrorCode.TargetPropertyReadOnly => 
                    ("MAPPIT008", "Target property is read only"),
                MappitErrorCode.NoSuitableConstructorFound => 
                    ("MAPPIT009", "No suitable constructor found"),
                MappitErrorCode.IncompatibleSourceAndConstructorPropertyTypes => 
                    ("MAPPIT010", "Incompatible type found for target constructor parameter"),
                MappitErrorCode.EnumTypeMismatch => 
                    ("MAPPIT011", "Source and target enum/type mismatch"),
                MappitErrorCode.CannotReverseMapCustomMapping => 
                    ("MAPPIT012","Cannot add reverse maps to custom mappings"),
                _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null),
            };
        }
    }
}
