using System.Drawing;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageUrlGeneratorLambda;

public class Functions
{
    private readonly IAmazonS3 _s3Client;

    public Functions(IAmazonS3 s3)
    {
        _s3Client = s3;
    }

    [LambdaFunction]
    [HttpApi(LambdaHttpMethod.Get, "/presigned-url")]
    public async Task<IHttpResult> FunctionHandlerAsync(
        APIGatewayHttpApiV2ProxyRequest request)
    {
        const string bucketName = "553020909495-source-bucket";
        const string key = "test-image.png";
        
        try
        {
            var s3Request = new GetPreSignedUrlRequest()
            {
                BucketName = bucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.Now.AddMinutes(30),
                ContentType = "image/png",
            };

            var uploadUrl = await _s3Client.GetPreSignedURLAsync(s3Request);
            
            return HttpResults.Ok(new
            {
                key,
                uploadUrl
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while generating the url for uploading:'{ex.Message}'");
            return HttpResults.InternalServerError("Internal Server Error");
        }
    }
}
