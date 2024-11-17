using System.Net;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageOptimizerLambda;

public class Functions
{
    [LambdaFunction]
    public async Task FunctionHandlerAsync([FromServices] IAmazonS3 s3Client, S3EventNotification input, ILambdaContext context)
    {
        try
        {
            string uploadedImageName = input.Records.First().S3.Object.Key;
            long uploadedImageSizeInBytes = input.Records.First().S3.Object.Size;
            long maxImageSizeBytes = Convert.ToInt64(Environment.GetEnvironmentVariable("MAX_IMAGE_UPLOAD_SIZE_IN_BYTES"));
            string sourceBucketName = Environment.GetEnvironmentVariable("S3_SOURCE_BUCKET_NAME")!;
            string destinationBucketName = Environment.GetEnvironmentVariable("S3_DESTINATION_BUCKET_NAME")!;

            if (uploadedImageSizeInBytes > maxImageSizeBytes)
            {
                context.Logger.LogError($"Uploaded image ${uploadedImageName} is too large (${uploadedImageSizeInBytes} bytes) to be the maximum size of {maxImageSizeBytes} bytes.");
                return;
            }

            var response = await s3Client.GetObjectAsync(sourceBucketName, uploadedImageName);

            // Image conversion
            using var inputStream = response.ResponseStream;
            using var image = await Image.LoadAsync(inputStream);
            using var outputStream = new MemoryStream();
            await image.SaveAsync(
                outputStream,
                new WebpEncoder
                {
                    FileFormat = WebpFileFormatType.Lossless
                });

            outputStream.Position = 0;

            // Upload image to destination
            var putRequest = new PutObjectRequest
            {
                BucketName = destinationBucketName,
                Key = uploadedImageName.Split('.')[0]+".webp",
                InputStream = outputStream
            };
            await s3Client.PutObjectAsync(putRequest);

            context.Logger.LogInformation($"The image {uploadedImageName} was processed successfully.");
        }
        catch (Exception ex)
        {
            context.Logger.LogError("Unexpected error: \n" + ex.Message);
        }
    }
}