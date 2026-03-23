using System.Text;
using Asp.Versioning.ApiExplorer;
using AStar.Dev.Technical.Debt.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AStar.Dev.AspNet.Extensions.Swagger;

/// <summary>
///     Configures the Swagger generation options
/// </summary>
/// <remarks>
///     This allows API versioning to define a Swagger document per API version after the
///     <see cref="IApiVersionDescriptionProvider" /> service has been resolved from the service container
/// </remarks>
/// <remarks>
///     Initializes a new instance of the <see cref="ConfigureSwaggerOptions" /> class
/// </remarks>
/// <param name="provider">
///     The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger
///     documents
/// </param>
/// <param name="apiConfiguration">
///     The configured instance of <see cref="IOptions{ApiConfiguration}" /> to complete the Swagger configuration
/// </param>
public sealed class ConfigureSwaggerOptions(
    IApiVersionDescriptionProvider provider,
    IOptions<ApiConfiguration>     apiConfiguration) : IConfigureOptions<SwaggerGenOptions>
{
    /// <inheritdoc />
    public void Configure(SwaggerGenOptions options)
    {
        foreach(var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName,
                               CreateInfoForApiVersion(description, apiConfiguration.Value.OpenApiInfo.Title));
        }
    }

    [Refactor(2, 1, "Method is too long")]
    private OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description, string apiDescription)
    {
        var text = new StringBuilder(apiDescription);

        if(description.IsDeprecated)
        {
            _ = text.Append(" **** This API version has been deprecated. **** ");
        }

        if(description.SunsetPolicy is { } policy)
        {
            if(policy.Date is { } when)
            {
                _ = text.Append(" The API will be sunset on ")
                    .Append(when.Date.ToShortDateString())
                    .Append('.');
            }

            if(policy.HasLinks)
            {
                _ = text.AppendLine();

                var rendered = false;

                foreach(var link in policy.Links)
                {
                    if(link.Type != "text/html")
                    {
                        continue;
                    }

                    if(!rendered)
                    {
                        _ = text.Append("<h4>Links</h4><ul>");
                        rendered = true;
                    }

                    _ = text.Append("<li><a href=\"");
                    _ = text.Append(link.LinkTarget.OriginalString);
                    _ = text.Append("\">");

                    _ = text.Append(
                                StringSegment.IsNullOrEmpty(link.Title)
                                    ? link.LinkTarget.OriginalString
                                    : link.Title.ToString());

                    _ = text.Append("</a></li>");
                }

                if(rendered)
                {
                    _ = text.Append("</ul>");
                }
            }
        }

        _ = text.Append("<h4>Additional Information</h4>");
        apiConfiguration.Value.OpenApiInfo.Description = text.ToString();

        return apiConfiguration.Value.OpenApiInfo;
    }
}
