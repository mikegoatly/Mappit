using System;

namespace Mappit.Examples
{
    /// <summary>
    /// Example program showing how to use the Mappit library
    /// </summary>
    public static class Program
    {
        public static void Main()
        {
            // Create a mapper instance
            var mapper = new Mapper();

            // Create a source object
            var foo = new Foo
            {
                Id = 1,
                Name = "Test Object",
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            // Map to target type using the strongly-typed method
            var fooRepresentation = mapper.Map(foo);

            Console.WriteLine($"Mapped object: Id={fooRepresentation.Id}, Name={fooRepresentation.Name}");
        }
    }
}