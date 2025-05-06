using System;

namespace Mappit.Examples
{
    /// <summary>
    /// Example source model
    /// </summary>
    public class Foo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Example target model
    /// </summary>
    public class FooRepresentation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}