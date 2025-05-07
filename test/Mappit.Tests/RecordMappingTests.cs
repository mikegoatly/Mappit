using Xunit;

namespace Mappit.Tests
{
    public class RecordMappingTests
    {
        [Fact]
        public void MapRecordToClass()
        {
            var mapper = new RecordToClassMapper();
            var source = new Root("root", new Level1("level1", new Level2("level2")));
            var target = mapper.Map(source);
            Assert.Equal(source.Name, target.Name);
            Assert.Equal(source.Level1.Name, target.Level1.Name);
            Assert.Equal(source.Level1.Level2.Name, target.Level1.Level2.Name);
        }

        [Fact]
        public void MapClassToRecord()
        {
            var mapper = new RecordToClassMapper();
            var source = new ClassRoot
            {
                Name = "root",
                Level1 = new ClassLevel1
                {
                    Name = "level1",
                    Level2 = new ClassLevel2
                    {
                        Name = "level2"
                    }
                }
            };

            var target = mapper.Map(source);
            Assert.Equal(source.Name, target.Name);
            Assert.Equal(source.Level1.Name, target.Level1.Name);
            Assert.Equal(source.Level1.Level2.Name, target.Level1.Level2.Name);
        }
    }

    public record Root(string Name, Level1 Level1);

    public record Level1(string Name, Level2 Level2);

    public record Level2(string Name);

    public class ClassRoot
    {
        public required string Name { get; set; }
        public required ClassLevel1 Level1 { get; set; }
    }

    public class ClassLevel1
    {
        public required string Name { get; set; }
        public required ClassLevel2 Level2 { get; set; }
    }

    public class ClassLevel2
    {
        public required string Name { get; set; }
    }

    [Mappit]
    public partial class RecordToClassMapper
    {
        public partial ClassRoot Map(Root source);
        public partial ClassLevel1 Map(Level1 source);
        public partial ClassLevel2 Map(Level2 source);
        public partial Root Map(ClassRoot source);
        public partial Level1 Map(ClassLevel1 source);
        public partial Level2 Map(ClassLevel2 source);
    }
}
