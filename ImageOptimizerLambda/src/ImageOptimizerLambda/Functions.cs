using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ImageOptimizerLambda.Exceptions;
using ImageOptimizerLambda.Services;
using Microsoft.Extensions.Configuration;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageOptimizerLambda;

public class Functions
{
    private record Parameters(
        int MaxImageDimension,
        long MaxImageSizeInBytes,
        string SourceBucketName,
        string DestinationBucketName);

    [LambdaFunction]
    public async Task FunctionHandlerAsync(
        [FromServices] IConfiguration config,
        [FromServices] IAmazonS3 s3Client,
        [FromServices] IImageOptimizerService imageOptimizerService,
        S3EventNotification input,
        ILambdaContext context)
    {
        Parameters parameters = GetParametersFromConfiguration(config);
        string uploadedImageName = input.Records.First().S3.Object.Key;
        long uploadedImageSizeInBytes = input.Records.First().S3.Object.Size;

        var imageStream = await DownloadImageFromSourceBucket(
            s3Client,
            parameters.SourceBucketName,
            uploadedImageName,
            uploadedImageSizeInBytes,
            parameters.MaxImageSizeInBytes);

        using var optimizedImageStream =
            await GetOptimizedImage(imageOptimizerService, imageStream, parameters.MaxImageDimension);

        var newImageName = imageOptimizerService.GenerateFileName(uploadedImageName);

        await UploadImageToDestinationBucket(s3Client, parameters.DestinationBucketName, newImageName,
            optimizedImageStream);

        context.Logger.LogInformation($"The image {uploadedImageName} was processed successfully.");
    }

    private Parameters GetParametersFromConfiguration(IConfiguration config) =>
        new(
            MaxImageDimension: Convert.ToInt32(config.GetRequiredSection("Settings:MaxImageDimension").Value),
            MaxImageSizeInBytes: Convert.ToInt64(config.GetRequiredSection("Settings:MaxImageSizeInBytes").Value),
            SourceBucketName: config.GetRequiredSection("S3_SOURCE_BUCKET_NAME").Value!,
            DestinationBucketName: config.GetRequiredSection("S3_DESTINATION_BUCKET_NAME").Value!
        );

    private async Task<Stream> DownloadImageFromSourceBucket(
        IAmazonS3 s3Client,
        string sourceBucketName,
        string imageName,
        long uploadedImageSizeInBytes,
        long maxImageSizeBytes)
    {
        try
        {
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
        int maxImageDimension
    )
    {
        try
        {
            return await imageOptimizerService.OptimizeImageAsync(originalImage, maxImageDimension);
        }
        catch (Exception e)
        {
            throw new ImageOptimizationException(
                $"Error occurred during the optimization of the image {originalImage}.", e);
        }
    }
}