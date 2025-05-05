using System.Collections.Generic;

namespace Mappit.Generator
{
    internal sealed class ValidatedMappingEnumInfo : ValidatedMappingInfo
    {
        public ValidatedMappingEnumInfo(MappingTypeInfo mappingTypeInfo)
            : base(mappingTypeInfo)
        {
        }

        public List<ValidatedMappingEnumMemberInfo> MemberMappings { get; } = new();
    }
}