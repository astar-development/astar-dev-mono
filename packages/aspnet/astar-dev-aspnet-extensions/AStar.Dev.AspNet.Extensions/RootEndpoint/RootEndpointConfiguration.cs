using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AStar.Dev.AspNet.Extensions.RootEndpoint;

/// <summary>
///     The <see cref="RootEndpointConfiguration" /> class
/// </summary>
public static class RootEndpointConfiguration
{
    /// <summary>
    ///     The ConfigureRootPage method will override the default API root to display an HTML page instead.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication" /> being extended</param>
    /// <param name="apiOrApplicationName">The API or Application name to use when returning the page</param>
    /// <returns>The original <see cref="WebApplication" /> to enable further method chaining</returns>
    public static WebApplication ConfigureRootPage(this WebApplication app, string apiOrApplicationName)
    {
        _ = app.MapGet("/", async context =>
                            {
                                context.Response.ContentType = "text/html";
                                await context.Response.WriteAsync(RootPage(apiOrApplicationName));
                            });

        return app;
    }

    private static string RootPage(string apiOrApplicationName)
    {
        const string swaggerV1Json = "/swagger/v1/swagger.json";

        return
            $"<h1>Welcome to the {apiOrApplicationName}.</h1>To access the V09 documentation, please use the: <a href='{swaggerV1Json}'>Swagger v1 json</a> link. <br /><br />" +
            $"Please be aware that more versions may be supported. <br /><br />"                                                                                                +
            $"If the Swagger UI is supported in this environment, please use the <a href='swagger/index.html'>Swagger UI</a> URL instead."                                      +
            $"<style>\nbody {{\n  background-color: black; color: white;\n}}\n</style>";
    }
}
