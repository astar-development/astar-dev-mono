namespace AStar.Dev.Test.Helpers.Minimal.Api;

public class TestEndpointRouteBuilder : IEndpointRouteBuilder
{
    public TestEndpointRouteBuilder()
    {
        var services = new ServiceCollection();

        services.AddApiVersioning(options =>
                                  {
                                      options.DefaultApiVersion                   = new(1, 0);
                                      options.AssumeDefaultVersionWhenUnspecified = true;
                                      options.ReportApiVersions                   = true;
                                  });

        ServiceProvider = services.BuildServiceProvider();
    }

    public IServiceProvider ServiceProvider { get; }

    public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();

    public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
}
