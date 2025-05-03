using System;
using Xunit;

namespace Mappit.Tests
{
    // Test enum types
    public enum Color
    {
        Red = 0,
        Green = 1,
        Blue = 2
    }

    public enum DisplayColor
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Yellow = 3
    }

    // Test models with enums
    public class ModelWithEnum
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }
    }

    public class DtoWithEnum
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DisplayColor PrimaryColor { get; set; }
        public DisplayColor SecondaryColor { get; set; }
    }

    // Update the TestMapper to include enum mappings
    public partial class TestMapperWithEnums : MapperBase
    {
        [MapType(typeof(ModelWithEnum), typeof(DtoWithEnum))]
        private TypeMapping modelToDto;

        [MapType(typeof(DtoWithEnum), typeof(ModelWithEnum))]
        private TypeMapping dtoToModel;
    }
}