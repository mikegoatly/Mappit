namespace Mappit.Generator
{
    /// <summary>
    /// Defines the kind of property mapping to perform
    /// </summary>
    internal enum PropertyKind
    {
        /// <summary>
        /// Standard property-to-property mapping
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Collection mapping, where items are mapped one-by-one
        /// </summary>
        Collection,

        /// <summary>
        /// Dictionary mapping, where keys and values are mapped
        /// </summary>
        Dictionary
    }
}