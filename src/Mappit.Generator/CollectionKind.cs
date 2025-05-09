namespace Mappit.Generator
{
    /// <summary>
    /// Defines the kind of type mapping to perform
    /// </summary>
    internal enum CollectionKind
    {
        /// <summary>
        /// Collection mapping, where items are mapped one-by-one.
        /// </summary>
        Collection,

        /// <summary>
        /// Dictionary mapping, where keys and values are mapped
        /// </summary>
        Dictionary
    }
}