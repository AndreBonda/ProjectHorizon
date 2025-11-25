using SixLabors.ImageSharp;

namespace ImageOptimizerLambda.Services;

public interface IImageOptimizerService
{
    Task<(string Id, string NameWithFilExt, MemoryStream Content)> OptimizeImageAsync(string imageId, Stream? inputStream, int maxImageDimension);
    
    /// <summary>
    /// Resizes the image. The longest side of the image is shortened based on the maxImageDimension parameter, and the
    /// other side is scaled proportionally to maintain the image's aspect ratio.
    /// </summary>
    /// <param name="image">The image to resize</param>
    /// <param name="maxImageDimension">Max image dimension in pixels.</param>
    void ResizeImage(Image image, int maxImageDimension);
}