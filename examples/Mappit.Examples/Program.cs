using System;
using System.Collections.Generic;

namespace Mappit.Examples
{
    /// <summary>
    /// Example mapper that demonstrates how to use Mappit
    /// </summary>
    [Mappit]
    internal sealed partial class DemoMapper
    {
        /// <summary>
        /// Maps a Foo object to FooRepresentation
        /// </summary>
        /// <param name="source">The source Foo object</param>
        /// <returns>A mapped FooRepresentation</returns>
        public partial IList<FooRepresentation> Map(IEnumerable<Foo> source);
    }

    internal static class Program
    {
        public static void Main()
        {
            // Create a mapper instance
            DemoMapper mapper = new DemoMapper();

            // Create source objects
            Foo[] foos = [
                new Foo(1, "Test Object", DateTime.Now, true),
                new Foo(2, "Another Object", DateTime.Now.AddDays(-1), false)
            ];

            // Map to target type using the strongly-typed method
            IList<FooRepresentation> fooRepresentations = mapper.Map(foos);

            foreach (var fooRepresentation in fooRepresentations)
            {
                Console.WriteLine($"Mapped object: Id={fooRepresentation.Id}, Name={fooRepresentation.Name}");
            }
        }
    }

    internal sealed record Foo(int Id, string Name, DateTime CreatedDate, bool IsActive);

    internal sealed class FooRepresentation(int id)
    {
        public int Id { get; } = id;
        public required string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}