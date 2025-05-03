using System;
using Xunit;

namespace Mappit.Tests
{
    // Test models for mapping
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
    }

    public class PersonDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Email { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public decimal Salary { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public decimal Salary { get; set; }
    }

    // Our test mapper implementation
    public partial class TestMapper : MapperBase
    {
        [MapType(typeof(Person), typeof(PersonDto))]
        private TypeMapping personToDto;

        [MapType(typeof(PersonDto), typeof(Person))]
        private TypeMapping dtoToPerson;

        [MapType(typeof(Employee), typeof(EmployeeDto))]
        private TypeMapping employeeToDto;
    }
}