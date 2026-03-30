using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AStar.Dev.Api.HealthChecks;

/// <summary>
///     The <see cref="HealthCheckExtensions" /> class contains the relevant method(s) to configure the endpoints.
/// </summary>
public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
                                                                          {
                                                                              WriteIndented          = true,
                                                                              DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                                                              PropertyNamingPolicy   = JsonNamingPolicy.CamelCase
                                                                          };

    /// <summary>
    ///     The <see cref="ConfigureHealthCheckEndpoints" /> method will add a basic health/live and health/ready endpoint.
    /// </summary>
    /// <param name="app">An instance of <see cref="WebApplication" /> to configure</param>
    /// <returns>The original <see cref="WebApplication" /> to facilitate further method chaining</returns>
    public static WebApplication ConfigureHealthCheckEndpoints(this WebApplication app)
    {
        _ = app.MapHealthChecks("/health/live",
                                new()
                                {
                                    ResponseWriter = WriteHealthCheckResponseAsync,
                                    ResultStatusCodes =
                                    {
                                        [HealthStatus.Degraded]  = StatusCodes.Status424FailedDependency,
                                        [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                                        [HealthStatus.Unhealthy] = StatusCodes.Status500InternalServerError
                                    },
                                    Predicate = _ => true
                                });

        _ = app.MapHealthChecks("/health/ready",
                                new()
                                {
                                    ResponseWriter = WriteHealthCheckResponseAsync,
                                    ResultStatusCodes =
                                    {
                                        [HealthStatus.Degraded]  = StatusCodes.Status424FailedDependency,
                                        [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                                        [HealthStatus.Unhealthy] = StatusCodes.Status500InternalServerError
                                    },
                                    Predicate = _ => true
                                });

        return app;
    }

    private static Task WriteHealthCheckResponseAsync(
        HttpContext  httpContext,
        HealthReport healthReport)
    {
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        var dependencyHealthChecks = healthReport.Entries.Select(static entry => new HealthStatusResponse
                                                                                 {
                                                                                     Name        = entry.Key,
                                                                                     Description = entry.Value.Description,
                                                                                     Status      = entry.Value.Status.ToString(),
                                                                                     DurationInMilliseconds = entry.Value.Duration
                                                                                                                   .TotalMilliseconds,
                                                                                     Data      = entry.Value.Data,
                                                                                     Exception = entry.Value.Exception?.Message
                                                                                 });

        var healthCheckResponse = new
                                  {
                                      Status                                = healthReport.Status.ToString(),
                                      TotalCheckExecutionTimeInMilliseconds = healthReport.TotalDuration.TotalMilliseconds,
                                      DependencyHealthChecks                = dependencyHealthChecks
                                  };

        string responseString = JsonSerializer.Serialize(healthCheckResponse, _jsonSerializerOptions);

        return httpContext.Response.WriteAsync(responseString);
    }
}
