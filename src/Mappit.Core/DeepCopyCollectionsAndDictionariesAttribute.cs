using System;

namespace Mappit
{
    /// <summary>
    /// Attribute to control how collections and dictionaries are mapped, either by reference or by
    /// cloning.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DeepCopyCollectionsAndDictionariesAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="DeepCopyCollectionsAndDictionariesAttribute"/>
        /// </summary>
        /// <param name="deepCopyCollectionsAndDictionaries">
        /// <inheritdoc cref="MappitAttribute.DeepCopyCollectionsAndDictionaries"/>
        /// </param>
        public DeepCopyCollectionsAndDictionariesAttribute(bool deepCopyCollectionsAndDictionaries = true)
        {
            DeepCopyCollectionsAndDictionaries = deepCopyCollectionsAndDictionaries;
        }

        /// <inheritdoc cref="MappitAttribute.DeepCopyCollectionsAndDictionaries" />
        public bool DeepCopyCollectionsAndDictionaries { get; }
    }
}
