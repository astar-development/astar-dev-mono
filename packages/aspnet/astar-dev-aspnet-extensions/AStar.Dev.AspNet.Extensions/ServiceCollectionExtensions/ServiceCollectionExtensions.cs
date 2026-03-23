using System.Net;
using System.Text.Json.Serialization;
using Asp.Versioning;
using AStar.Dev.Api.Usage.Sdk;
using AStar.Dev.AspNet.Extensions.Handlers;
using AStar.Dev.AspNet.Extensions.Swagger;
using AStar.Dev.Technical.Debt.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AStar.Dev.AspNet.Extensions.ServiceCollectionExtensions;

/// <summary>
///     The <see cref="ServiceCollectionExtensions" /> class contains the method(s) available to configure the pipeline in
///     a consistent manner
/// </summary>
public static class ServiceCollectionExtensions
{
    private static ApiUsageConfiguration ApiUsageConfiguration { get; set; } = new() { UserName = "NotSet", Password = string.Empty, HostName = "NotSet", QueueName = "NotSet" };

    /// <summary>
    ///     The <see cref="ConfigureUi" /> will do exactly what it says on the tin...this time around, this is for the UI
    /// </summary>
    /// <param name="services">
    ///     An instance of the <see cref="IServiceCollection" /> interface that will be configured with the Global Exception
    ///     Handler, and the controllers (a UI isn't much use without them...)
    /// </param>
    /// <returns>
    ///     The original <see cref="IServiceCollection" /> to facilitate method chaining
    /// </returns>
    /// <seealso href="AddApiConfiguration">
    /// </seealso>
    [Refactor(1, 1, "migrate UI-specific tasks here")]
    public static IServiceCollection ConfigureUi(this IServiceCollection services)
    {
        _ = services
            .AddControllers();

        return services;
    }

    /// <summary>
    ///     The <see cref="AddApiConfiguration" /> will do exactly what it says on the tin...this time around, this is for the API
    /// </summary>
    /// <param name="services">
    ///     An instance of the <see cref="IServiceCollection" /> interface that will be configured with the Global Exception
    ///     Handler, and the controllers (a UI isn't much use without them...)
    /// </param>
    /// <param name="configurationManager">
    ///     An instance of the <see cref="ConfigurationManager" /> used during API Configuration
    /// </param>
    /// <returns>
    ///     The original <see cref="IServiceCollection" /> to facilitate method chaining
    /// </returns>
    /// <seealso href="ConfigureUi">
    /// </seealso>
    [Refactor(1, 1, "too long a method")]
#pragma warning disable CA1506 // Class coupling is too high
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services, ConfigurationManager configurationManager)
    {
        _ = services
            .AddOptions<ApiUsageConfiguration>()
            .Bind(configurationManager.GetSection(ApiUsageConfiguration.ConfigurationSectionName));

        ApiUsageConfiguration = services.BuildServiceProvider().GetRequiredService<IOptions<ApiUsageConfiguration>>().Value;
        _ = services.AddProblemDetails();
        services.CreateValidatedApiConfiguration(configurationManager);
        _ = services.AddEndpointsApiExplorer();
        _ = services.AddHealthChecks();
        _ = services.AddExceptionHandler<GlobalExceptionHandler>();

        _ = services.AddApiVersioning(options =>
                                  {
                                      options.UnsupportedApiVersionStatusCode = (int)HttpStatusCode.NotImplemented;
                                      options.ReportApiVersions = true;
                                      options.ApiVersionReader = new QueryStringApiVersionReader("version");
                                  })
                .AddApiExplorer(options =>
                                {
                                    options.GroupNameFormat = "'v'VVV";
                                    options.SubstituteApiVersionInUrl = true;
                                })
                .EnableApiVersionBinding();

        _ = services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        _ = services.AddControllers().AddJsonOptions(jsonoptions => jsonoptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        _ = services.AddSwaggerGen(options =>
                               {
                                   options.AddSecurityDefinition("Bearer", new()
                                   {
                                       Description = """
                                                                                             JWT Authorization header using the Bearer scheme. \r\n\r\n 
                                                                                               Enter 'just' your token in the text input below.
                                                                                               \r\n\r\nExample: '12345etc'
                                                                                             """,
                                       Name = "Authorization",
                                       In = ParameterLocation.Header,
                                       Type = SecuritySchemeType.Http,
                                       Scheme = "Bearer"
                                   });

                                   options.AddSecurityRequirement(new()
                                                                  {
                                                                      {
                                                                          new()
                                                                          {
                                                                              Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                                                                              Scheme    = "oauth2",
                                                                              Name      = "Bearer",
                                                                              In        = ParameterLocation.Header
                                                                          },
                                                                          new List<string>()
                                                                      }
                                                                  });
                               });

        return services;
    }

    private static void CreateValidatedApiConfiguration(this IServiceCollection services,
                                                        ConfigurationManager configurationManager) =>
        services
            .AddOptions<ApiConfiguration>()
            .Bind(configurationManager.GetSection(ApiConfiguration.ConfigurationSectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
}

#pragma warning restore CA1506 // Class coupling is too high
