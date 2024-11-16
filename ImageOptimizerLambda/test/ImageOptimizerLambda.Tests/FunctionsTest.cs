using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.S3.Util;
using Xunit;

namespace ImageOptimizerLambda.Tests;

public class FunctionTest
{
    public FunctionTest()
    {
    }

    [Fact]
    public void TestGetMethod()
    {
        // Arrange
        var context = new TestLambdaContext();
        var functions = new Functions();
        string s3Event = """
                         {
                             "Records": [
                                 {
                                     "s3": {
                                         "bucket": {
                                             "name": "example-bucket"
                                         },
                                         "object": {
                                             "key": "example-file.txt"
                                         }
                                     }
                                 }
                             ]
                         } 
                         """;

        var response = functions.FunctionHandler(S3EventNotification.ParseJson(s3Event), context);

        Assert.Equal("example-file.txt",response);
    }
}
