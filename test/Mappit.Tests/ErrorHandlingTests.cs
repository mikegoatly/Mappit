using System;
using Xunit;

namespace Mappit.Tests
{
    public class ErrorHandlingTests
    {
        private readonly IMapper _mapper;

        public ErrorHandlingTests()
        {
            _mapper = new TestMapper();
        }

        [Fact]
        public void Map_UnmappedType_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var unmappedObject = new UnmappedType { Id = 1, Name = "Test" };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _mapper.Map<PersonDto>(unmappedObject));
            
            Assert.Contains("No mapping defined from", exception.Message);
        }

        [Fact]
        public void Map_UsingIncorrectDestinationType_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var person = new Person
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "j@doe.com"
            };

            // Act & Assert - Trying to map Person to EmployeeDto (no mapping defined)
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _mapper.Map<EmployeeDto>(person));
            
            Assert.Contains("No mapping defined from", exception.Message);
        }

        [Fact]
        public void Map_UsingExplicitTypesWithNoMapping_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var person = new Person
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "j@doe.com"
            };

            // Act & Assert - Trying to map Person to EmployeeDto (no mapping defined)
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _mapper.Map<Person, EmployeeDto>(person));
            
            Assert.Contains("No mapping defined from", exception.Message);
        }
    }

    // A class with no mapping defined
    public class UnmappedType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}