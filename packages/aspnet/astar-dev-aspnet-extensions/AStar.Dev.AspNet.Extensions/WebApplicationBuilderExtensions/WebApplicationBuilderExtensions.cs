using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace AStar.Dev.AspNet.Extensions.WebApplicationBuilderExtensions;

/// <summary>
///     The <see href=""></see> class containing applicable extensions for the <see href="WebApplicationBuilder"></see>
///     class
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    ///     This method will, unsurprisingly, disable the "Server" Header for all responses from the Kestrel Server
    /// </summary>
    /// <param name="builder">
    ///     The instance of <see href="WebApplicationBuilder"></see> to configure
    /// </param>
    /// <returns>
    ///     The original instance of the <see href="WebApplicationBuilder"></see> to facilitate method chaining (AKA fluent
    ///     configuration)
    /// </returns>
    public static WebApplicationBuilder DisableServerHeader(this WebApplicationBuilder builder)
    {
        _ = builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

        return builder;
    }
}
