using System;
using System.Collections;
using System.Collections.Generic;

namespace Mappit.Generator
{
    internal sealed class ValidatedMappingMemberInfoSet : IEnumerable<ValidatedMappingMemberInfo>
    {
        private readonly Dictionary<string, ValidatedMappingMemberInfo> mappings = new(StringComparer.OrdinalIgnoreCase);

        public ValidatedMappingMemberInfo this[string name] => mappings[name];

        public void Add(ValidatedMappingMemberInfo mapping)
        {
            this.mappings.Add(mapping.TargetProperty.Name, mapping);
        }

        public IEnumerator<ValidatedMappingMemberInfo> GetEnumerator()
        {
            return mappings.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsTargetName(string name)
        {
            return mappings.ContainsKey(name);
        }
    }
}
