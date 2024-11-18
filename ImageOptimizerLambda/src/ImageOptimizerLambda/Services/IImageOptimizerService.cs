namespace ImageOptimizerLambda.Services;

public interface IImageOptimizerService
{
    /// <summary>
    /// Convert the image to WebP format.
    /// </summary>
    Task<MemoryStream> ConvertImageToWebpFormat(Stream? inputStream);
    string GetWebpImageName(string originalName);
}