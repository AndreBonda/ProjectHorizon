using Amazon.Lambda.Annotations;
using Amazon.S3;
using ImageOptimizerLambda.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ImageOptimizerLambda;

[LambdaStartup]
public class Startup
{
    private IConfiguration Configuration { get; }

    public Startup()
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appSettings.json")
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Services for Lambda functions can be registered in the services dependency injection container in this method.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Configuration);
        services.AddScoped<IAmazonS3, AmazonS3Client>();
        services.AddScoped<IImageOptimizerService, ImageOptimizerService>();
    }
}