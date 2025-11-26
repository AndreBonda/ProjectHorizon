using Amazon.Lambda.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    
    public HostApplicationBuilder ConfigureHostBuilder()
    {
        var hostBuilder = new HostApplicationBuilder();
        hostBuilder.Services.AddSingleton(Configuration);
        hostBuilder.Services.AddAWSService<Amazon.S3.IAmazonS3>();
        hostBuilder.Services.AddAWSService<Amazon.DynamoDBv2.IAmazonDynamoDB>();
        return hostBuilder;
    }
}