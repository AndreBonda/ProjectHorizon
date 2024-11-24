using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ImageOptimizerLambda.Services;

public class ImageOptimizerService : IImageOptimizerService
{
    /// <inheritdoc />
    public async Task<MemoryStream> OptimizeImageAsync(Stream? inputStream, int maxImageDimension)
    {
        if (inputStream is null)
            return new MemoryStream();

        using var image = await Image.LoadAsync(inputStream);
        ResizeImage(image, maxImageDimension);
        var webpImage = await ConvertToWebpAsync(image);
        return webpImage;
    }

    /// <inheritdoc />
    public string GenerateFileName(string originalName) => originalName.Split('.')[0] + ".webp";

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