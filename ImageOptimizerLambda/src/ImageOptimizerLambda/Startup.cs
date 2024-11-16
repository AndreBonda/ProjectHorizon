using Amazon.Lambda.Annotations;
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
    }
}