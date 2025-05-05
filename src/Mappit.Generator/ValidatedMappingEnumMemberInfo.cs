using Microsoft.CodeAnalysis;

namespace Mappit.Generator
{
    internal sealed class ValidatedMappingEnumMemberInfo
    {
        public ValidatedMappingEnumMemberInfo(IFieldSymbol sourceField, IFieldSymbol targetField)
        {
            this.SourceField = sourceField;
            this.TargetField = targetField;
        }
        public IFieldSymbol SourceField { get; }
        public IFieldSymbol TargetField { get; }
    }
}