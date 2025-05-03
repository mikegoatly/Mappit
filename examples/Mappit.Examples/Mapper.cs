using System;

namespace Mappit.Examples
{
    /// <summary>
    /// Example mapper that demonstrates how to use Mappit
    /// </summary>
    public partial class Mapper : MapperBase
    {
        private TypeMapping<Foo, FooRepresentation> foo;
    }
}