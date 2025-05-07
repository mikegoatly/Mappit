using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal sealed class ValidatedMappingTypeInfo : ValidatedMappingInfo
    {
        public ValidatedMappingTypeInfo(MappingTypeInfo mappingTypeInfo)
            : base(mappingTypeInfo)
        {
        }

        /// <summary>
        /// A lookup of the source and target member mappings. Keyed by the target member name.
        /// </summary>
        /// <remarks>
        /// We use an ordinal ignore case comparer so we can match property names to parameter names of constructors.
        /// However, this will mean that if a property is named "Name" and another property is named "name" we will
        /// not be able to distinguish between them.
        /// </remarks>
        public Dictionary<string, ValidatedMappingMemberInfo> MemberMappings { get; } = new(StringComparer.OrdinalIgnoreCase);
        public IMethodSymbol? Constructor { get; internal set; }
    }
}