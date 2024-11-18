using SixLabors.ImageSharp;

namespace ImageOptimizerLambda.Services;

public interface IImageOptimizerService
{
    /// <summary>
    /// Optimizes an image by resizing it to fit within the specified maximum dimensions and converting it to WebP format.
    /// </summary>
    /// <param name="inputStream">The input stream containing the image to optimize.</param>
    /// <param name="maxImageDimension">The maximum allowed dimension (in pixels) for the longest side of the image.</param>
    /// <returns>A memory stream containing the optimized image in WebP format.</returns>
    Task<MemoryStream> OptimizeImage(Stream? inputStream, int maxImageDimension);

    /// <summary>
    /// Returns the name with the current file extension replaced by .webp.
    /// </summary>
    string GenerateWebpFileName(string originalName);

    /// <summary>
    /// Resizes the image. The longest side of the image is shortened based on the maxImageDimension parameter, and the
    /// other side is scaled proportionally to maintain the image's aspect ratio.
    /// </summary>
    /// <param name="image">The image to resize</param>
    /// <param name="maxImageDimension">Max image dimension in pixels.</param>
    void ResizeImage(Image image, int maxImageDimension);
}