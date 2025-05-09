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
    }
}
