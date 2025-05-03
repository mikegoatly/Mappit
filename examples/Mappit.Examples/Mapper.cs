using System;

namespace Mappit.Examples
{
    /// <summary>
    /// Example mapper that demonstrates how to use Mappit
    /// </summary>
    public partial class Mapper : MapperBase
    {
        [MapType(typeof(Foo), typeof(FooRepresentation))]
        private TypeMapping foo;
        
        // No need for any constructor or manual initialization
        // MappingBase will automatically call the generated Initialize methods
    }
}