using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ImageOptimizerLambda.Exceptions;
using ImageOptimizerLambda.Services;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageOptimizerLambda;

public class Functions
{
    [LambdaFunction]
    public async Task FunctionHandlerAsync(
        [FromServices] IAmazonS3 s3Client,
        [FromServices] IImageOptimizerService imageOptimizerService,
        S3EventNotification input,
        ILambdaContext context)
    {
        string uploadedImageName = input.Records.First().S3.Object.Key;
        long uploadedImageSizeInBytes = input.Records.First().S3.Object.Size;
        string sourceBucketName = Environment.GetEnvironmentVariable("S3_SOURCE_BUCKET_NAME")!;
        string destinationBucketName = Environment.GetEnvironmentVariable("S3_DESTINATION_BUCKET_NAME")!;
        int maxImageDimension = int.Parse(Environment.GetEnvironmentVariable("MAX_IMAGE_DIMENSION")!);

        var imageStream = await DownloadImageFromSourceBucket(
            s3Client,
            sourceBucketName,
            uploadedImageName,
            uploadedImageSizeInBytes);

        using var optimizedImageStream =
            await GetOptimizedImage(imageOptimizerService, imageStream, maxImageDimension);

        var newImageName = imageOptimizerService.GenerateFileName(uploadedImageName);
        await UploadImageToDestinationBucket(s3Client, destinationBucketName, newImageName, optimizedImageStream);

        context.Logger.LogInformation($"The image {uploadedImageName} was processed successfully.");
    }

    private async Task<Stream> DownloadImageFromSourceBucket(
        IAmazonS3 s3Client,
        string sourceBucketName,
        string imageName,
        long uploadedImageSizeInBytes)
    {
        try
        {
            var maxImageSizeBytes =
                Convert.ToInt64(Environment.GetEnvironmentVariable("MAX_IMAGE_UPLOAD_SIZE_IN_BYTES"));

            if (uploadedImageSizeInBytes > maxImageSizeBytes)
            {
                throw new TooLargeImageException(
                    $"Uploaded image ${imageName} is too large (${uploadedImageSizeInBytes} bytes) to be the maximum size of {maxImageSizeBytes} bytes.");
            }

            var response = await s3Client.GetObjectAsync(sourceBucketName, imageName);
            return response.ResponseStream;
        }
        catch (Exception e)
        {
            throw new ImageDownloadFromSourceBucketException(
                $"Error occurred during the download of the image {imageName}.", e);
        }
    }

    private async Task UploadImageToDestinationBucket(
        IAmazonS3 s3Client,
        string destinationBucketName,
        string imageName,
        Stream imageStream)
    {
        try
        {
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = destinationBucketName,
                Key = imageName,
                InputStream = imageStream
            });
        }
        catch (Exception e)
        {
            throw new ImageUploadToDestinationBucketException(
                $"Error occurred during the upload of the image {imageName}.", e);
        }
    }

    private async Task<MemoryStream> GetOptimizedImage(
        IImageOptimizerService imageOptimizerService,
        Stream originalImage,
        int maxImageDimensionInBytes
    )
    {
        try
        {
            return await imageOptimizerService.OptimizeImageAsync(originalImage, maxImageDimensionInBytes);
        }
        catch (Exception e)
        {
            throw new ImageOptimizationException(
                $"Error occurred during the optimization of the image {originalImage}.", e);
        }
    }
}