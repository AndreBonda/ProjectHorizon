using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace ImageOptimizerLambda.Services;

public class ImageOptimizerService : IImageOptimizerService
{
    public async Task<MemoryStream> ConvertImageToWebpFormat(Stream? inputStream)
    {
        if(inputStream is null)
            return new MemoryStream();

        using var image = await Image.LoadAsync(inputStream);
        var outputStream = new MemoryStream();
        await image.SaveAsync(
            outputStream,
            new WebpEncoder
            {
                FileFormat = WebpFileFormatType.Lossless
            });

        outputStream.Position = 0;
        return outputStream;
    }

    public string GetWebpImageName(string originalName) => originalName.Split('.')[0] + ".webp";
}