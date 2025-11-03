using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.S3;
using Amazon.S3.Model;
using NSubstitute;
using Microsoft.Extensions.Configuration;

namespace ImageUrlGeneratorLambda.Tests;

public class Tests
{
    private IAmazonS3 _mockS3Client;
    private IConfiguration _mockConfig;
    private Functions _functions;
    private TestLambdaContext _context;
    
    [SetUp]
    public void Setup()
    {
        _mockS3Client = Substitute.For<IAmazonS3>();
        _mockConfig = Substitute.For<IConfiguration>();
        _context = new TestLambdaContext();
        _mockConfig.GetRequiredSection("S3_SOURCE_BUCKET_NAME").Value.Returns("test-bucket");
        _mockConfig.GetRequiredSection("Settings:UrlExpirationMinutes").Value.Returns("30");
        _functions = new Functions(_mockConfig, _mockS3Client);
    }

    [TearDown]
    public void TearDown()
    {
        _mockS3Client?.Dispose();
    }
    
    [Test]
    public async Task FunctionHandlerAsync_Success_ReturnsOkWithPresignedUrl()
    {
        // Arrange
        var request = new APIGatewayHttpApiV2ProxyRequest();
        var expectedUrl = "https://test-bucket.s3.amazonaws.com/test-key?presigned-params";
        _mockS3Client.GetPreSignedURLAsync(Arg.Any<GetPreSignedUrlRequest>())
            .Returns(expectedUrl);

        // Act
        var result = await _functions.FunctionHandlerAsync(request);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
    
    [Test]
    public async Task GetParametersFromConfiguration_InvalidExpirationMinutes_ThrowsArgumentException()
    {
        // Arrange
        var request = new APIGatewayHttpApiV2ProxyRequest();
        _mockConfig.GetRequiredSection("Settings:UrlExpirationMinutes").Value.Returns("0");

        // Act
        var result = await _functions.FunctionHandlerAsync(request);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }
    
    [Test]
    public async Task GetParametersFromConfiguration_EmptyBucketName_ThrowsArgumentException()
    {
        // Arrange
        var request = new APIGatewayHttpApiV2ProxyRequest();
        _mockConfig.GetRequiredSection("S3_SOURCE_BUCKET_NAME").Value.Returns(" ");

        // Act
        var result = await _functions.FunctionHandlerAsync(request);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }
}
