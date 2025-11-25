using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ImageOptimizerLambda.Services;
using Microsoft.Extensions.Configuration;
using Xunit;
using NSubstitute;

namespace ImageOptimizerLambda.Tests;

public class FunctionTest
{
    private readonly IConfiguration _configuration;
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IImageOptimizerService _imageOptimizerService;
    private readonly ILambdaContext _lambdaContext;
    private readonly Functions _functions;

    public FunctionTest()
    {
        _configuration = Substitute.For<IConfiguration>();
        SetupConfiguration();
        _s3Client = Substitute.For<IAmazonS3>();
        _dynamoDbClient = Substitute.For<IAmazonDynamoDB>();
        _imageOptimizerService = Substitute.For<IImageOptimizerService>();
        _lambdaContext = Substitute.For<ILambdaContext>();
        _lambdaContext.Logger.Returns(Substitute.For<ILambdaLogger>());
        _functions = new Functions(_configuration, _s3Client, _dynamoDbClient);
    }

    private void SetupConfiguration()
    {
        _configuration.GetRequiredSection("Settings:MaxImageDimension").Value.Returns("1000");
        _configuration.GetRequiredSection("Settings:MaxImageSizeInBytes").Value.Returns("1000");
        _configuration.GetRequiredSection("Settings:UrlExpirationMinutes").Value.Returns("30");
        _configuration.GetRequiredSection("S3_SOURCE_BUCKET_NAME").Value.Returns("source-bucket-name");
        _configuration.GetRequiredSection("S3_DESTINATION_BUCKET_NAME").Value.Returns("destination-bucket-name");
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

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _functions.FunctionHandlerAsync(
                _imageOptimizerService,
                s3Event,
                _lambdaContext);
        });
    }

    [Fact]
    public async Task FunctionHandlerAsync_UploadsWebpImage()
    {
        // Arrange
        _s3Client
            .GetObjectAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new GetObjectResponse());
        _imageOptimizerService
            .GenerateFileName(Arg.Any<string>())
            .Returns("example-file.webp");
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
                                  "size": 500
                              }
                          }
                      }
                  ]
              }
              """);

        // Act
        await _functions.FunctionHandlerAsync(
            _imageOptimizerService,
            s3Event,
            _lambdaContext);

        // Assert
        await _s3Client
            .Received(1)
            .PutObjectAsync(Arg.Is<PutObjectRequest>(o => o.Key == "example-file.webp"));
        _lambdaContext.Logger
            .Received(1)
            .LogInformation(Arg.Is<string>(s => s.Contains("successfully")));
    }
}