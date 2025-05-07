using System;

namespace Mappit.Examples
{
    /// <summary>
    /// Example mapper that demonstrates how to use Mappit
    /// </summary>
    [Mappit]
    public partial class Mapper
    {
        /// <summary>
        /// Maps a Foo object to FooRepresentation
        /// </summary>
        /// <param name="source">The source Foo object</param>
        /// <returns>A mapped FooRepresentation</returns>
        public partial FooRepresentation Map(Foo source);
    }
}