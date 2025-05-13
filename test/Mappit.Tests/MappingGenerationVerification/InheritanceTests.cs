using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Mappit.Tests.MappingGenerationVerification
{
    public class InheritanceTests
    {
        [Fact]
        public void MapObjectsFromSourceWithInheritance_ShouldMapBaseClassProperties()
        {
            // Arrange
            var source = new ConcreteClass { Id = 1, Name = "Test" };
            var mapper = new Mapper();
            // Act
            var result = mapper.Map(source);
            // Assert
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.Name, result.Name);
        }

        [Fact]
        public void MapObjectsToTargetWithInheritance_ShouldMapBaseClassProperties()
        {
            // Arrange
            var source = new MappedClass(1, "Test");
            var mapper = new Mapper();
            // Act
            var result = mapper.Map(source);
            // Assert
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.Name, result.Name);
        }
    }

    internal abstract class BaseClass
    {
        public required string Name { get; init; }
    }

    internal sealed class ConcreteClass : BaseClass
    {
        public int Id { get; init; }
    }

    internal sealed record MappedClass(int Id, string Name);

    [Mappit]
    internal sealed partial class Mapper
    {
        [ReverseMap]
        public partial MappedClass Map(ConcreteClass source);
    }
}
