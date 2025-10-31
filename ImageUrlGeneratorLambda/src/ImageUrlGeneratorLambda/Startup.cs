using Amazon.Lambda.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[LambdaStartup]
public class Startup
{
    public HostApplicationBuilder ConfigureHostBuilder()
    {
        var hostBuilder = new HostApplicationBuilder();
        hostBuilder.Services.AddAWSService<Amazon.S3.IAmazonS3>();
        return hostBuilder;
    }
}