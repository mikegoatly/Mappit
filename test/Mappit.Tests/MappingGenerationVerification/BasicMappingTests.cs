using System;
using Xunit;

namespace Mappit.Tests.MappingGenerationVerification
{
    public class BasicMappingTests
    {
        private readonly ITestMapper _mapper;

        public BasicMappingTests()
        {
            _mapper = new TestMapper();
        }

        [Fact]
        public void Map_PersonToDto_ShouldMapAllSharedProperties()
        {
            // Arrange
            var person = new Person
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                BirthDate = new DateTime(1980, 1, 1),
                Age = 43,
                Email = "john.doe@example.com"
            };

            // Act
            var dto = _mapper.Map(person);

            // Assert
            Assert.Equal(person.Id, dto.Id);
            Assert.Equal(person.FirstName, dto.FirstName);
            Assert.Equal(person.LastName, dto.LastName);
            Assert.Equal(person.BirthDate, dto.BirthDate);
            Assert.Equal(person.Email, dto.Email);
            Assert.Null(person.Height);
        }
        
        [Fact]
        public void Map_DtoToPerson_ShouldMapSharedProperties()
        {
            // Arrange
            var dto = new PersonDto
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                BirthDate = new DateTime(1985, 5, 5),
                Email = "jane.smith@example.com"
            };

            // Act
            var person = _mapper.Map(dto);

            // Assert
            Assert.Equal(dto.Id, person.Id);
            Assert.Equal(dto.FirstName, person.FirstName);
            Assert.Equal(dto.LastName, person.LastName);
            Assert.Equal(dto.BirthDate, person.BirthDate);
            Assert.Equal(dto.Email, person.Email);
            Assert.Equal(0, person.Age); // Non-mapped property should have default value
            Assert.Null(person.Height);
        }

        [Fact]
        public void Map_NullSource_ShouldReturnDefault()
        {
            // Act & Assert
            Assert.Null(_mapper.Map((Person?)null));
        }
    }

    internal sealed class Person
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
        public required string Email { get; set; }
        public int? Height { get; set; }
    }

    internal sealed class PersonDto
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
        public required string Email { get; set; }
        public int? Height { get; set; }
    }

    [Mappit]
    internal sealed partial class TestMapper
    {
        [ReverseMap]
        public partial PersonDto Map(Person source);
    }
}