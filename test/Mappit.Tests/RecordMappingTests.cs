using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            var target = mapper.Map<ClassRoot>(source);
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

            var target = mapper.Map<Root>(source);
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
        public string Name { get; set; }
        public ClassLevel1 Level1 { get; set; }
    }

    public class ClassLevel1
    {
        public string Name { get; set; }
        public ClassLevel2 Level2 { get; set; }
    }

    public class ClassLevel2
    {
        public string Name { get; set; }
    }

    [Mappit]
    public partial class RecordToClassMapper
    {
        TypeMapping<Root, ClassRoot> rootRecordToClass;
        TypeMapping<Level1, ClassLevel1> level1RecordToClass;
        TypeMapping<Level2, ClassLevel2> level2RecordToClass;
        TypeMapping<ClassRoot, Root> classToRecordRoot;
        TypeMapping<ClassLevel1, Level1> classToRecordLevel1;
        TypeMapping<ClassLevel2, Level2> classToRecordLevel2;
    }
}
