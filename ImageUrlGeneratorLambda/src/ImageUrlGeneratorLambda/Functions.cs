using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
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

    public Functions(IConfiguration configuration, IAmazonS3 s3)
    {
        _s3Client = s3;
        _config = configuration;
    }

    [LambdaFunction]
    [HttpApi(LambdaHttpMethod.Get, "/presigned-url")]
    public async Task<IHttpResult> FunctionHandlerAsync(
        APIGatewayHttpApiV2ProxyRequest request)
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
            
            return HttpResults.Ok(new
            {
                imageId,
                uploadUrl
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while generating the url for uploading:'{ex.Message}'");
            return HttpResults.InternalServerError("Internal Server Error");
        }
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
