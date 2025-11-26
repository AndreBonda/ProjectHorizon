using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
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
    private IAmazonDynamoDB _dynamoDbClient;
    private IConfiguration _mockConfig;
    private Functions _functions;
    private TestLambdaContext _context;

    [SetUp]
    public void Setup()
    {
        _mockS3Client = Substitute.For<IAmazonS3>();
        _mockConfig = Substitute.For<IConfiguration>();
        _dynamoDbClient = Substitute.For<IAmazonDynamoDB>();
        _context = new TestLambdaContext();
        _mockConfig.GetRequiredSection("S3_SOURCE_BUCKET_NAME").Value.Returns("test-bucket");
        _mockConfig.GetRequiredSection("Settings:UrlExpirationMinutes").Value.Returns("30");
        _functions = new Functions(_mockConfig, _mockS3Client, _dynamoDbClient);
    }

    [TearDown]
    public void TearDown()
    {
        _mockS3Client.Dispose();
        _dynamoDbClient.Dispose();
    }

    [Test]
    public async Task FunctionHandlerAsync_Success_ReturnsOkWithPresignedUrl()
    {
        // Arrange
        var expectedUrl = "https://test-bucket.s3.amazonaws.com/test-key?presigned-params";
        _mockS3Client.GetPreSignedURLAsync(Arg.Any<GetPreSignedUrlRequest>())
            .Returns(expectedUrl);

        // Act
        var result = await _functions.FunctionHandlerAsync(new APIGatewayHttpApiV2ProxyRequest()
            {
                RawPath = "/stage/presigned-url",
                RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext()
                {
                    Stage = "stage",
                }
            },
            _context);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(result.Body, Contains.Substring(expectedUrl));
    }

    [Test]
    public async Task GetParametersFromConfiguration_InvalidExpirationMinutes_ThrowsArgumentException()
    {
        // Arrange
        var request = new APIGatewayHttpApiV2ProxyRequest();
        _mockConfig.GetRequiredSection("Settings:UrlExpirationMinutes").Value.Returns("0");

        // Act
        var result = await _functions.GetPresignedUrl();

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task GetParametersFromConfiguration_EmptyBucketName_ThrowsArgumentException()
    {
        // Arrange
        _mockConfig.GetRequiredSection("S3_SOURCE_BUCKET_NAME").Value.Returns(" ");

        // Act
        var result = await _functions.GetPresignedUrl();

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(500));


    }

    [Test]
    public async Task FunctionHandler_WithNonExistingEndpoint_Returns404NotFound()
    {
        // Arrange
        var request = new APIGatewayHttpApiV2ProxyRequest()
        {
            RawPath = "/stage/non-existing-endpoint",
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext()
            {
                Stage = "stage",
            }
        };

        // Act
        var result = await _functions.FunctionHandlerAsync(request, _context);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task GetOptimizedImage_WithoutImageId_Returns400BadRequest()
    {
        // Arrange
        var request = new APIGatewayHttpApiV2ProxyRequest()
        {
            RawPath = "/stage/optimized-image/",
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext()
            {
                Stage = "stage",
            },
            PathParameters = new Dictionary<string, string>()
            {
                {
                    "imageId", string.Empty
                }
            }
        };

        // Act
        var result = await _functions.FunctionHandlerAsync(request, _context);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(400));
        Assert.That(result.Body, Contains.Substring("imageId required"));
    }

    [Test]
    public async Task GetOptimizedImage_WithNonExistingImageId_Returns404NotFound()
    {
        // Arrange
        _dynamoDbClient
            .GetItemAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, AttributeValue>>())
            .Returns(new GetItemResponse
                {
                    Item = null
                }
            );
        var request = new APIGatewayHttpApiV2ProxyRequest()
        {
            RawPath = "/stage/optimized-image/",
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext()
            {
                Stage = "stage",
            },
            PathParameters = new Dictionary<string, string>()
            {
                {
                    "imageId", "non-existing-image-id"
                }
            }
        };

        // Act
        var result = await _functions.FunctionHandlerAsync(request, _context);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(404));
        Assert.That(result.Body, Contains.Substring("Image not found"));
    }

    [Test]
    public async Task GetOptimizedImage_ReturnsPresignedUrl()
    {
        // Arrange
        _dynamoDbClient
            .GetItemAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, AttributeValue>>())
            .Returns(new GetItemResponse
                {
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        {
                            "ImageId", new AttributeValue()
                            {
                                S = "an-image-id"
                            }
                        },
                        {
                            "Status", new AttributeValue()
                            {
                                S = "Completed"
                            }
                        },
                        {
                            "DownloadImageUrl", new AttributeValue()
                            {
                                S = "a-presigned-url"
                            }
                        },
                        {
                            "DateTime", new AttributeValue()
                            {
                                S = "datetime"
                            }
                        },

                    }
                }
            );
        var request = new APIGatewayHttpApiV2ProxyRequest()
        {
            RawPath = "/stage/optimized-image/",
            RequestContext = new APIGatewayHttpApiV2ProxyRequest.ProxyRequestContext()
            {
                Stage = "stage",
            },
            PathParameters = new Dictionary<string, string>()
            {
                {
                    "imageId", "an-image-id"
                }
            }
        };

        // Act
        var result = await _functions.FunctionHandlerAsync(request, _context);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(result.Body, Contains.Substring("an-image-id"));
        Assert.That(result.Body, Contains.Substring("Completed"));
        Assert.That(result.Body, Contains.Substring("a-presigned-url"));
    }
}
