using System;

namespace Mappit
{
    /// <summary>
    /// The marker attribute to add to a mapping class.
    /// 
    /// <code>
    /// [Mappit]
    /// public partial class MyMappings
    /// {
    ///     public partial MyDto Map(MyModel model);
    /// }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MappitAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to ignore missing properties on the target type.
        /// </summary>
        /// <remarks>
        /// Default is false, meaning the absence of a property in the target will generate an error.
        /// </remarks>
        public bool IgnoreMissingPropertiesOnTarget { get; set; } = false;

        /// <summary>
        /// Whether collections and dictionaries should always be shallow copied, even if their
        /// element types are identical. e.g. A property of type <c>List&lt;string&gt;</c> by default will
        /// have its reference copied to the target object, but when <see cref="DeepCopyCollectionsAndDictionaries"/> 
        /// is set to <c>true</c>, a <b>new</b> List&lt;string&gt; will be created with the same content.
        /// You would only need to prevent mutations of a mapped list affecting the list in the object
        /// it was copied <b>from</b>.
        /// This has no effect on collections/dictionaries for which the elements require mapping - in this case
        /// a new collection/dictionary <b>will always</b> be created.
        /// </summary>
        /// <remarks>
        /// Default is false, meaning collections and dictionaries are copied by reference when possible.
        /// </remarks>
        public bool DeepCopyCollectionsAndDictionaries { get; set; } = false;
    }
}
