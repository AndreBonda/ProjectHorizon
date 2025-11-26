using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageUrlGeneratorLambda;

public class Functions
{
    private record Parameters(
        string BucketName,
        int ExpiresInMinutes);

    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _config;
    private readonly IAmazonDynamoDB _dynamoDbClient;

    public Functions(IConfiguration configuration, IAmazonS3 s3, IAmazonDynamoDB dynamoDbClient)
    {
        _s3Client = s3;
        _dynamoDbClient = dynamoDbClient;
        _config = configuration;
    }

    [LambdaFunction]
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandlerAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var stage = request.RequestContext.Stage!;
        var path = request.RawPath.Substring(stage.Length + 1);

        if (path == "/presigned-url")
        {
            return await GetPresignedUrl();
        }
        
        if (path.StartsWith("/optimized-image"))
        {
            string imageId = request.PathParameters.ContainsKey("imageId") ? request.PathParameters["imageId"] : string.Empty;
            if (imageId == string.Empty)
            {
                return new APIGatewayHttpApiV2ProxyResponse
                {
                    StatusCode = 400,
                    Body = JsonSerializer.Serialize(new { message = "imageId required" }),
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
            return await GetOptimizedImageAsync(request.PathParameters["imageId"]);
        }
        
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 404,
            Body = JsonSerializer.Serialize(new { message = "Not Found" }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
    
    public async Task<APIGatewayHttpApiV2ProxyResponse> GetPresignedUrl()
    {
        try
        {
            var parameter = GetParametersFromConfiguration();
            var imageId = Guid.NewGuid();

            var s3Request = new GetPreSignedUrlRequest()
            {
                BucketName = parameter.BucketName,
                Key = imageId.ToString(),
                Verb = HttpVerb.PUT,
                Expires = DateTime.Now.AddMinutes(parameter.ExpiresInMinutes),
                ContentType = "image/png",
            };

            var uploadUrl = await _s3Client.GetPreSignedURLAsync(s3Request);
            
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 200,
                Body = JsonSerializer.Serialize(new { imageId, uploadUrl}),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while generating the url for uploading:'{ex.Message}'");
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 500,
                Body = JsonSerializer.Serialize(new { message = "Something went wrong" }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
    
    public async Task<APIGatewayHttpApiV2ProxyResponse> GetOptimizedImageAsync(string imageId)
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

        var imageRecord = response.Item;
        if (imageRecord == null || imageRecord.Count == 0)
        {
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 404,
                Body = JsonSerializer.Serialize(new { message = "Image not found" }),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
        
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new
            {
                ImageId = imageRecord.GetValueOrDefault("ImageId")?.S,
                Status = imageRecord.GetValueOrDefault("Status")?.S,
                Url = imageRecord.GetValueOrDefault("DownloadImageUrl")?.S,
                DateTime = imageRecord.GetValueOrDefault("DateTime")?.S,
            }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }

    private Parameters GetParametersFromConfiguration()
    {
        string? bucketName = _config.GetRequiredSection("S3_SOURCE_BUCKET_NAME").Value;
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        var expiresInMinutes = Convert.ToInt32(_config.GetRequiredSection("Settings:UrlExpirationMinutes").Value);
        if (expiresInMinutes <= 0) throw new ArgumentException("ExpiresInMinutes must be greater than 0");
        return new Parameters(bucketName, expiresInMinutes);
    }
}
