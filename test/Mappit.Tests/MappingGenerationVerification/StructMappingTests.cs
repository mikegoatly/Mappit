using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Mappit.Tests.MappingGenerationVerification
{
    public class StructMappingTests
    {
        [Fact]
        public void MappingStructToStruct_ShouldMapProperties()
        {
            var person1Dto = new PersonStructDto(1, "Bob", null);
            var person2Dto = new PersonStructDto(2, "Jenz", new DateTime(2000, 1, 2));

            ITestMapperWithStructs mapper = new TestMapperWithStructs();

            var person1 = mapper.Map(person1Dto);
            var person2 = mapper.Map(person2Dto);

            Assert.Equivalent(person1, new PersonStruct(1, "Bob", null));
            Assert.Equivalent(person2, new PersonStruct(2, "Jenz", new DateTime(2000, 1, 2)));
        }

        [Fact]
        public void MappingNullableStructToStruct_ShouldMapProperties()
        {
            var personDto = new PersonStructDto(1, "Bob", null);

            ITestMapperWithStructs mapper = new TestMapperWithStructs();

            var person = mapper.MapNullable(new Nullable<PersonStructDto>(personDto));

            Assert.Equivalent(person, new PersonStruct(1, "Bob", null));
        }

        [Fact]
        public void MappingNullNullableStructToStruct_ShouldThrowException()
        {
            ITestMapperWithStructs mapper = new TestMapperWithStructs();
            Assert.Throws<ArgumentNullException>("source", () => mapper.MapNullable(null));
        }

        [Fact]
        public void MappingStructToNullableStruct_ShouldMapProperties()
        {
            var personDto = new PersonStructDto(1, "Bob", null);

            ITestMapperWithStructs mapper = new TestMapperWithStructs();

            var person = mapper.MapNullableReturn(personDto);

            Assert.Equivalent(person, new PersonStruct(1, "Bob", null));
        }
    }

    [Mappit]
    public partial class TestMapperWithStructs
    {
        [ReverseMap]
        public partial PersonStruct Map(PersonStructDto source);

        public partial PersonStruct? MapNullableReturn(PersonStructDto source);

        public partial PersonStruct MapNullable(PersonStructDto? source);
    }

    public record struct PersonStruct(int Id, string Name, DateTime? BirthDate);

    public record struct PersonStructDto(int Id, string Name, DateTime? BirthDate);
}