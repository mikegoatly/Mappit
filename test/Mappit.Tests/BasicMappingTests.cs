using System;
using Xunit;

namespace Mappit.Tests
{


    public class BasicMappingTests
    {
        private readonly IMapper _mapper;

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
            var dto = _mapper.Map<PersonDto>(person);

            // Assert
            Assert.Equal(person.Id, dto.Id);
            Assert.Equal(person.FirstName, dto.FirstName);
            Assert.Equal(person.LastName, dto.LastName);
            Assert.Equal(person.BirthDate, dto.BirthDate);
            Assert.Equal(person.Email, dto.Email);
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
            var person = _mapper.Map<Person>(dto);

            // Assert
            Assert.Equal(dto.Id, person.Id);
            Assert.Equal(dto.FirstName, person.FirstName);
            Assert.Equal(dto.LastName, person.LastName);
            Assert.Equal(dto.BirthDate, person.BirthDate);
            Assert.Equal(dto.Email, person.Email);
            Assert.Equal(0, person.Age); // Non-mapped property should have default value
        }

        [Fact]
        public void Map_UsingGenericOverload_ShouldMapCorrectly()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 3,
                FirstName = "Bob",
                LastName = "Johnson",
                Department = "Engineering",
                Salary = 85000
            };

            // Act - Using the generic overload that specifies source and destination types
            var dto = _mapper.Map<Employee, EmployeeDto>(employee);

            // Assert
            Assert.Equal(employee.Id, dto.Id);
            Assert.Equal(employee.FirstName, dto.FirstName);
            Assert.Equal(employee.LastName, dto.LastName);
            Assert.Equal(employee.Department, dto.Department);
            Assert.Equal(employee.Salary, dto.Salary);
        }

        [Fact]
        public void Map_NullSource_ShouldReturnDefault()
        {
            // Act & Assert
            Assert.Null(_mapper.Map<PersonDto>(null));
            Assert.Null(_mapper.Map<Person, PersonDto>(null));
        }
    }
}