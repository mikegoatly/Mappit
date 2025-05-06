using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal sealed class ValidatedMappingEnumMemberInfo
    {
        public ValidatedMappingEnumMemberInfo(IFieldSymbol sourceField, IFieldSymbol targetField)
        {
            SourceField = sourceField;
            TargetField = targetField;
        }
        public IFieldSymbol SourceField { get; }
        public IFieldSymbol TargetField { get; }
    }
}