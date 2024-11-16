using System.Net;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.S3.Util;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageOptimizerLambda;

public class Functions
{
    [LambdaFunction]
    public string FunctionHandler(S3EventNotification input, ILambdaContext context)
    {
        context.Logger.LogInformation(input.Records.First().S3.Object.Key);
        context.Logger.LogInformation(Environment.GetEnvironmentVariable("S3_SOURCE_BUCKET_NAME"));
        context.Logger.LogInformation(Environment.GetEnvironmentVariable("S3_DESTINATION_BUCKET_NAME"));
        return input.Records.First().S3.Object.Key;
    }
}
