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

        [Fact]
        public void Map_EnumOutOfRange_ShouldConvertBetweenEnums()
        {
            // Arrange - Using a value that exists only in the destination enum
            var dto = new DtoWithEnum
            {
                Id = 3,
                Name = "Enum Out of Range Test",
                PrimaryColor = DisplayColor.Yellow // This doesn't exist in Color enum
            };

            // Act
            var model = _mapper.Map<ModelWithEnum>(dto);
            
            // Act again - Map back to DTO
            var mappedBackDto = _mapper.Map<DtoWithEnum>(model);

            // Assert
            // When casting to an enum that doesn't have the value, it's preserved as the integer value
            Assert.Equal(3, (int)model.PrimaryColor); // Yellow = 3
            
            // When mapping back, we should get the original enum value
            Assert.Equal(DisplayColor.Yellow, mappedBackDto.PrimaryColor);
        }
    }
}