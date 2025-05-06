using System;

namespace Mappit.Examples
{
    /// <summary>
    /// Example mapper that demonstrates how to use Mappit
    /// </summary>
    [Mappit]
    public partial class Mapper
    {
        private TypeMapping<Foo, FooRepresentation> foo;
    }
}