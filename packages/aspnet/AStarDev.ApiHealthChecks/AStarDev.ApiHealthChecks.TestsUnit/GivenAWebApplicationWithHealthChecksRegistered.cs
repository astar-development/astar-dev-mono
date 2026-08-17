using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.Api.HealthChecks;

[TestSubject(typeof(HealthCheckExtensions))]
public class GivenAWebApplicationWithHealthChecksRegistered
{
    [Fact]
    public void when_health_check_endpoints_are_configured_then_health_check_service_is_registered()
    {
        var webApplication = WebApplication.CreateBuilder();
        webApplication.Services.AddHealthChecks();

        var sut = webApplication.Build().ConfigureHealthCheckEndpoints();

        sut.Services.GetServices<HealthCheckService>().Count().ShouldBe(1);
    }
}
