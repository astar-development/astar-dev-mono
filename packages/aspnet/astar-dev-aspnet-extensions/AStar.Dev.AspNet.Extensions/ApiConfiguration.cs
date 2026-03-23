using Microsoft.OpenApi.Models;

namespace AStar.Dev.AspNet.Extensions;

/// <summary>
///     The <see cref="ApiConfiguration" /> class which is used to load the Api Configuration
/// </summary>
public sealed class ApiConfiguration
{
    /// <summary>
    ///     The static Configuration Section Name which controls where DI looks for the API Configuration
    /// </summary>
    public const string ConfigurationSectionName = "ApiConfiguration";

    /// <summary>
    ///     The <see cref="OpenApiInfo" /> used to configure Swagger
    /// </summary>
    public OpenApiInfo OpenApiInfo { get; set; } = new();
}
