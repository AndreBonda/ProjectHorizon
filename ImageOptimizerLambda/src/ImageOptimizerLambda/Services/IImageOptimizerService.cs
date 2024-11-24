using SixLabors.ImageSharp;

namespace ImageOptimizerLambda.Services;

public interface IImageOptimizerService
{
    /// <summary>
    /// Reduces the memory usage of the original image by applying compression and resizing it to fit within the specified maximum dimensions.
    /// </summary>
    /// <param name="inputStream">The input stream containing the image to optimize.</param>
    /// <param name="maxImageDimension">The maximum allowed dimension (in pixels) for the longest side of the image.</param>
    /// <returns>A memory stream containing the optimized image.</returns>
    Task<MemoryStream> OptimizeImageAsync(Stream? inputStream, int maxImageDimension);

    /// <summary>
    /// Returns the name with its file extension.
    /// </summary>
    string GenerateFileName(string originalName);

    /// <summary>
    /// Resizes the image. The longest side of the image is shortened based on the maxImageDimension parameter, and the
    /// other side is scaled proportionally to maintain the image's aspect ratio.
    /// </summary>
    /// <param name="image">The image to resize</param>
    /// <param name="maxImageDimension">Max image dimension in pixels.</param>
    void ResizeImage(Image image, int maxImageDimension);
}