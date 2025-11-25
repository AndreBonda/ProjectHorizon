using ImageOptimizerLambda.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ImageOptimizerLambda.Tests;

public class ImageOptimizerServiceTest
{
    private readonly ImageOptimizerService _imageOptimizerService = new();

    [Fact]
    public async Task OptimizeImage_ReturnsAndEmptyStream_WhenTheInputIsNull()
    {
        // Arrange & Act
        var image = await _imageOptimizerService.OptimizeImageAsync("image-id", null, 100);

        // Assert
        Assert.True(image.Content.Length == 0);
    }

    [Fact]
    public async Task OptimizeImage_ConvertsTheImage()
    {
        // Arrange
        using var inputImage = new Image<Rgba32>(5, 5);
        var inputStream = new MemoryStream();
        await inputImage.SaveAsPngAsync(inputStream);
        inputStream.Position = 0;

        // Act
        var image = await _imageOptimizerService.OptimizeImageAsync("image-id", inputStream, 100);

        // Assert
        Assert.True(image.Content.Length > 0);
        Assert.Equal("image-id.webp", image.NameWithFilExt);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void ResizeImage_ThrowsException_WhenMaxDimensionIsZeroOrNegative(int invalidMaxImageDimension)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _imageOptimizerService.ResizeImage(new Image<Rgba32>(100, 100), invalidMaxImageDimension));
    }

    [Fact]
    public void ResizeImage_DoesNotResize_WhenImageIsSmallerThanMaxDimension()
    {
        // Arrange
        var image = new Image<Rgba32>(50, 50);
        int maxImageDimension = 100;

        // Act
        _imageOptimizerService.ResizeImage(image, maxImageDimension);

        // Assert
        Assert.Equal(50, image.Width);
        Assert.Equal(50, image.Height);
    }

    [Fact]
    public void ResizeImage_ResizesWidthAndScaleHeight_WhenWidthIsGreaterThanHeight()
    {
        // Arrange
        var image = new Image<Rgba32>(200, 100);
        int maxImageDimension = 100;

        // Act
        _imageOptimizerService.ResizeImage(image, maxImageDimension);

        // Assert
        Assert.Equal(100, image.Width);
        Assert.Equal(50, image.Height);
    }

    [Fact]
    public void ResizeImage_ResizesHeightAndScaleWidth_WhenHeightIsGreaterThanWidth()
    {
        // Arrange
        var image = new Image<Rgba32>(90, 150);
        int maxImageDimension = 100;

        // Act
        _imageOptimizerService.ResizeImage(image, maxImageDimension);

        // Assert
        Assert.Equal(60, image.Width);
        Assert.Equal(100, image.Height);
    }
}