using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ImageOptimizerLambda.Services;
using Microsoft.Extensions.Configuration;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageOptimizerLambda;

public class Functions
{
    private readonly IConfiguration _config;
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonDynamoDB _dynamoDbClient;

    private record Parameters(
        int MaxImageDimension,
        long MaxImageSizeInBytes,
        string SourceBucketName,
        string DestinationBucketName,
        int UrlExpirationMinutes);

    public Functions(IConfiguration configuration, IAmazonS3 s3Client, IAmazonDynamoDB dynamoDbClient)
    {
        _config = configuration;
        _s3Client = s3Client;
        _dynamoDbClient = dynamoDbClient;
    }

    [LambdaFunction]
    public async Task FunctionHandlerAsync(
        [FromServices] IImageOptimizerService imageOptimizerService,
        S3EventNotification input,
        ILambdaContext context)
    {
        string imageId = string.Empty;

        try
        {
            Parameters parameters = GetParametersFromConfiguration(_config);
            imageId = input.Records.First().S3.Object.Key;
            long uploadedImageSizeInBytes = input.Records.First().S3.Object.Size;

            if (await CheckIfImageAlreadyProcessedSuccessfullyAsync(imageId))
            {
                context.Logger.LogInformation($"Image {imageId} was already processed successfully.");
                return;
            }

            var imageStream = await DownloadImageFromSourceBucketAsync(
                parameters.SourceBucketName,
                imageId,
                uploadedImageSizeInBytes,
                parameters.MaxImageSizeInBytes);

            var optimizedImage = await imageOptimizerService.OptimizeImageAsync(imageId, imageStream, parameters.MaxImageDimension);

            await using (optimizedImage.Content)
            {
                await UploadImageToDestinationBucket(
                    parameters.DestinationBucketName,
                    optimizedImage.NameWithFilExt,
                    optimizedImage.Content);
            }

            var downloadUrl = await GenerateDownloadPresignedUrlAsync(
                parameters.DestinationBucketName,
                optimizedImage.NameWithFilExt,
                parameters.UrlExpirationMinutes);

            await RecordImageProcessingAsync(imageId, downloadUrl);
            context.Logger.LogInformation($"The image {imageId} was processed successfully.");
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Failed to process image {imageId}: {e.Message}");

            try
            {
                await RecordImageProcessingFailureAsync(imageId);
            }
            catch (Exception innerExc)
            {
                context.Logger.LogError($"Failed to record image processing failure for the image {imageId}: {innerExc.Message}");
            }
            throw;
        }
    }

    private Parameters GetParametersFromConfiguration(IConfiguration config) =>
        new(
            MaxImageDimension: Convert.ToInt32(config.GetRequiredSection("Settings:MaxImageDimension").Value),
            MaxImageSizeInBytes: Convert.ToInt64(config.GetRequiredSection("Settings:MaxImageSizeInBytes").Value),
            UrlExpirationMinutes: Convert.ToInt32(config.GetRequiredSection("Settings:UrlExpirationMinutes").Value),
            SourceBucketName: config.GetRequiredSection("S3_SOURCE_BUCKET_NAME").Value!,
            DestinationBucketName: config.GetRequiredSection("S3_DESTINATION_BUCKET_NAME").Value!
        );

    /// <summary>
    /// Provides idempotency by checking if the image was already processed successfully.
    /// </summary>
    private async Task<bool> CheckIfImageAlreadyProcessedSuccessfullyAsync(string imageId)
    {
        var response = await _dynamoDbClient.GetItemAsync(
            "Images",
            new Dictionary<string, AttributeValue>()
            {
                {
                    "ImageId", new AttributeValue
                    {
                        S = imageId
                    }
                }
            });

        var item = response.Item;
        if (item == null || item.Count == 0)
        {
            return false;
        }
        return item.GetValueOrDefault("Status")?.S == "Completed";
    }

    private async Task<Stream> DownloadImageFromSourceBucketAsync(
        string sourceBucketName,
        string imageId,
        long uploadedImageSizeInBytes,
        long maxImageSizeBytes)
    {

        if (uploadedImageSizeInBytes > maxImageSizeBytes)
        {
            throw new ArgumentException(
                $"Image {imageId} too large ({uploadedImageSizeInBytes} > {maxImageSizeBytes} bytes).");
        }

        var response = await _s3Client.GetObjectAsync(sourceBucketName, imageId);
        return response.ResponseStream;
    }

    private async Task UploadImageToDestinationBucket(
        string destinationBucketName,
        string imageName,
        Stream imageStream)
    {

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = destinationBucketName,
            Key = imageName,
            InputStream = imageStream
        });
    }

    private async Task<string> GenerateDownloadPresignedUrlAsync(string destinationBucketName, string imageName, int expiration)
    {
        return await _s3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = destinationBucketName,
            Key = imageName,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(expiration)
        });
    }

    private async Task RecordImageProcessingAsync(string imageId, string downloadUrl)
    {
        await _dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = "Images",
            Item = new Dictionary<string, AttributeValue>()
            {
                {
                    "ImageId", new AttributeValue
                    {
                        S = imageId
                    }
                },
                {
                    "Status", new AttributeValue
                    {
                        S = "Completed"
                    }
                },
                {
                    "DownloadImageUrl", new AttributeValue
                    {
                        S = downloadUrl
                    }
                },
                {
                    "DateTime", new AttributeValue
                    {
                        S = DateTime.Now.ToString("O")
                    }
                }
            }
        });
    }

    private async Task RecordImageProcessingFailureAsync(string imageId)
    {
        await _dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = "Images",
            Item = new Dictionary<string, AttributeValue>()
            {
                {
                    "ImageId", new AttributeValue
                    {
                        S = imageId
                    }
                },
                {
                    "Status", new AttributeValue
                    {
                        S = "Error"
                    }
                },
                {
                    "DateTime", new AttributeValue
                    {
                        S = DateTime.Now.ToString("O")
                    }
                }
            }
        });
    }
}
