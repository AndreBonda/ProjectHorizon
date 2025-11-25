using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ImageOptimizerLambda.Services;

public class ImageOptimizerService : IImageOptimizerService
{
    /// <inheritdoc />
    public async Task<(string Id, string NameWithFilExt, MemoryStream Content)> OptimizeImageAsync(string imageId, Stream? inputStream, int maxImageDimension)
    {
        string nameWithExt = imageId + ".webp";
        var stream = await OptimizeImageAsync(inputStream, maxImageDimension);
        return (imageId, nameWithExt, stream);
    }
    
    /// <inheritdoc />
    public void ResizeImage(Image image, int maxImageDimension)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxImageDimension);

        if (image.Width <= maxImageDimension && image.Height <= maxImageDimension)
            return;

        if (image.Width > image.Height)
        {
            image.Mutate(x => x.Resize(maxImageDimension, 0, KnownResamplers.Lanczos3));
        }
        else
        {
            image.Mutate(x => x.Resize(0, maxImageDimension, KnownResamplers.Lanczos3));
        }
    }
    
    /// <summary>
    /// Reduces the memory usage of the original image by applying compression and resizing it to fit within the specified maximum dimensions.
    /// </summary>
    /// <param name="inputStream">The input stream containing the image to optimize.</param>
    /// <param name="maxImageDimension">The maximum allowed dimension (in pixels) for the longest side of the image.</param>
    /// <returns>A memory stream containing the optimized image.</returns>
    private async Task<MemoryStream> OptimizeImageAsync(Stream? inputStream, int maxImageDimension)
    {
        if (inputStream is null)
            return new MemoryStream();

        using var image = await Image.LoadAsync(inputStream);
        ResizeImage(image, maxImageDimension);
        var webpImage = await ConvertToWebpAsync(image);
        return webpImage;
    }

    private async Task<MemoryStream> ConvertToWebpAsync(Image originalImage)
    {
        var outputStream = new MemoryStream();
        await originalImage.SaveAsync(
            outputStream,
            new WebpEncoder
            {
                FileFormat = WebpFileFormatType.Lossless
            });

        outputStream.Position = 0;
        return outputStream;
    }
}