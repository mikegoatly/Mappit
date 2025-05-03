namespace Mappit.Tests
{
    public class Person
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
        public required string Email { get; set; }
    }

    public class PersonDto
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public required string Email { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Department { get; set; }
        public decimal Salary { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Department { get; set; }
        public decimal Salary { get; set; }
    }

    public partial class TestMapper : MapperBase
    {
        private TypeMapping<Person, PersonDto> personToDto;

        private TypeMapping<PersonDto, Person> dtoToPerson;

        private TypeMapping<Employee, EmployeeDto> employeeToDto;
    }
}