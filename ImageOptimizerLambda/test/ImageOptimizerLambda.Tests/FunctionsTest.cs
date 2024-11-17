using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.S3;
using Amazon.S3.Util;
using Castle.Core.Logging;
using Xunit;
using NSubstitute;

namespace ImageOptimizerLambda.Tests;

public class FunctionTest
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILambdaContext _lambdaContext;
    private readonly Functions _functions;

    public FunctionTest()
    {
        _s3Client = Substitute.For<IAmazonS3>();
        _lambdaContext = Substitute.For<ILambdaContext>();
        _lambdaContext.Logger.Returns(Substitute.For<ILambdaLogger>());
        _functions = new Functions();
    }

    [Fact]
    public async Task FunctionHandlerAsync_ReturnsAnError_IfTheFileIsTooLarge()
    {
        // Arrange
        long invalidSizeBytes = 1_000_000_000;
        S3EventNotification s3Event = S3EventNotification.ParseJson(
            $$"""
              {
                  "Records": [
                      {
                          "s3": {
                              "bucket": {
                                  "name": "example-bucket"
                              },
                              "object": {
                                  "key": "example-file.png",
                                  "size": {{invalidSizeBytes}}
                              }
                          }
                      }
                  ]
              }
              """);

        // Act
        await _functions.FunctionHandlerAsync(
            _s3Client,
            s3Event,
            _lambdaContext);

        // Assert
        _lambdaContext.Logger
            .Received(1)
            .LogError(Arg.Is<string>(x => x.Contains("too large")));
    }
}