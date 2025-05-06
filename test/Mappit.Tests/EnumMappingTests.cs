using System;
using Xunit;

namespace Mappit.Tests
{
    public class EnumMappingTests
    {
        private readonly IMapper _mapper;

        public EnumMappingTests()
        {
            _mapper = new TestMapperWithEnums();
        }

        [Fact]
        public void Map_ModelWithEnumToDto_ShouldMapEnumProperties()
        {
            // Arrange
            var model = new ModelWithEnum
            {
                Id = 1,
                Name = "Test Model",
                PrimaryColor = Color.Green,
                SecondaryColor = Color.Blue
            };

            // Act
            var dto = _mapper.Map<DtoWithEnum>(model);

            // Assert
            Assert.Equal(model.Id, dto.Id);
            Assert.Equal(model.Name, dto.Name);
            Assert.Equal(DisplayColor.Green, dto.PrimaryColor);
            Assert.Equal(DisplayColor.Blue, dto.SecondaryColor);
        }

        [Fact]
        public void Map_DtoWithEnumToModel_ShouldMapEnumProperties()
        {
            // Arrange
            var dto = new DtoWithEnum
            {
                Id = 2,
                Name = "Test DTO",
                PrimaryColor = DisplayColor.Red,
                SecondaryColor = DisplayColor.Blue
            };

            // Act
            var model = _mapper.Map<ModelWithEnum>(dto);

            // Assert
            Assert.Equal(dto.Id, model.Id);
            Assert.Equal(dto.Name, model.Name);
            Assert.Equal(Color.Red, model.PrimaryColor);
            Assert.Equal(Color.Blue, model.SecondaryColor);
        }
    }

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
    }

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

    [Mappit]
    public partial class TestMapperWithEnums
    {
        TypeMapping<ModelWithEnum, DtoWithEnum> modelToDto;

        TypeMapping<DtoWithEnum, ModelWithEnum> dtoToModel;

        TypeMapping<DisplayColor, Color> displayColorToColor;

        TypeMapping<Color, DisplayColor> colorToDisplayColor;
    }
}