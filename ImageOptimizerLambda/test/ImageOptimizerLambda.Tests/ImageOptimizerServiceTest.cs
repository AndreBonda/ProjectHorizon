using ImageOptimizerLambda.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ImageOptimizerLambda.Tests;

public class ImageOptimizerServiceTest
{
    private ImageOptimizerService _imageOptimizerService = new();

    [Fact]
    public async Task ConvertImageToWebpFormat_ReturnsAndEmptyStream_WhenTheInputIsNull()
    {
        // Arrange & Act
        var resultStream = await _imageOptimizerService.ConvertImageToWebpFormat(null);

        // Assert
        Assert.True(resultStream.Length == 0);
    }

    [Fact]
    public async Task ConvertImageToWebpFormat_ConvertsTheImage()
    {
        // Arrange
        using var inputImage = new Image<Rgba32>(5, 5);
        var inputStream = new MemoryStream();
        await inputImage.SaveAsPngAsync(inputStream);
        inputStream.Position = 0;

        // Act
        var resultStream = await _imageOptimizerService.ConvertImageToWebpFormat(inputStream);

        // Assert
        Assert.True(resultStream.Length > 0);
    }
}