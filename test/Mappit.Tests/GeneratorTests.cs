using System;
using Xunit;

namespace Mappit.Tests
{
    public class GeneratorTests
    {
        // These tests validate that the source generator works correctly
        // Since we can't directly test the generator, we test its effects

        [Fact]
        public void Generator_ShouldCreateConstructorWithInitialization()
        {
            // This test validates that the source generator correctly creates
            // a constructor that initializes all mappings
            
            // Arrange & Act - Simply creating the mapper should initialize mappings
            var mapper = new TestMapper();
            
            // Get a person object
            var person = new Person
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            };
            
            // Assert - If initialization happened, we should be able to map
            var dto = mapper.Map<PersonDto>(person);
            Assert.Equal(person.Id, dto.Id);
            Assert.Equal(person.Email, dto.Email);
            
            // We can also verify the reverse mapping works
            var personDto = new PersonDto
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@example.com"
            };
            
            var mappedPerson = mapper.Map<Person>(personDto);
            Assert.Equal(personDto.Id, mappedPerson.Id);
            Assert.Equal(personDto.Email, mappedPerson.Email);
        }
        
        [Fact]
        public void Generator_ShouldMapAllTypeProperties()
        {
            // This test validates that the source generator properly maps all shared properties
            
            // Arrange
            var employee = new Employee
            {
                Id = 3,
                FirstName = "Bob",
                LastName = "Johnson",
                Department = "Engineering",
                Salary = 85000m
            };
            
            var mapper = new TestMapper();
            
            // Act
            var dto = mapper.Map<EmployeeDto>(employee);
            
            // Assert - All properties should be mapped
            Assert.Equal(employee.Id, dto.Id);
            Assert.Equal(employee.FirstName, dto.FirstName);
            Assert.Equal(employee.LastName, dto.LastName);
            Assert.Equal(employee.Department, dto.Department);
            Assert.Equal(employee.Salary, dto.Salary);
        }
    }
}