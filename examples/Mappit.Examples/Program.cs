using System;

namespace Mappit.Examples
{
    /// <summary>
    /// Example program showing how to use the Mappit library
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create a mapper instance
            IMapper mapper = new Mapper();

            // Create a source object
            var foo = new Foo
            {
                Id = 1,
                Name = "Test Object",
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            // Map to destination type
            var fooRepresentation = mapper.Map<FooRepresentation>(foo);

            // Alternatively, you can use the generic version
            var fooRepresentation2 = mapper.Map<Foo, FooRepresentation>(foo);

            Console.WriteLine($"Mapped object: Id={fooRepresentation.Id}, Name={fooRepresentation.Name}");
        }
    }
}