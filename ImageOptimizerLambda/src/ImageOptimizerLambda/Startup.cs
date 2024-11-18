using Amazon.Lambda.Annotations;
using Amazon.S3;
using ImageOptimizerLambda.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImageOptimizerLambda;

[LambdaStartup]
public class Startup
{
    /// <summary>
    /// Services for Lambda functions can be registered in the services dependency injection container in this method.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IAmazonS3, AmazonS3Client>();
        services.AddScoped<IImageOptimizerService, ImageOptimizerService>();
    }
}