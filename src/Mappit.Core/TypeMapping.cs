namespace Mappit
{
    /// <summary>
    /// Represents a mapping between two types
    /// </summary>
    public abstract class TypeMapping
    {
        /// <summary>
        /// Maps the source object to the destination type
        /// </summary>
        /// <param name="source">Source object</param>
        /// <returns>Mapped destination object</returns>
        public abstract object Map(object source);
    }
}