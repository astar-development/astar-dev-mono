using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.AspNet.Extensions.ConfigurationManagerExtensions;

/// <summary>
///     The <see cref="ConfigurationManagerExtensions" /> contains the current extensions for the
///     <see cref="ConfigurationManager" /> class from Microsoft.
/// </summary>
public static class ConfigurationManagerExtensions
{
    /// <summary>
    ///     The GetValidatedConfigurationSection will retrieve the specified configuration settings and return the requested
    ///     configuration object.
    /// </summary>
    /// <param name="configuration">The instance of <see cref="IConfigurationManager" /> to configure</param>
    /// <param name="configurationSectionKey">The name of the configuration section to map</param>
    /// <typeparam name="T">The type of the configuration settings to configure and return</typeparam>
    /// <returns>The original <see cref="IConfigurationManager" /> to facilitate further call chaining</returns>
    public static T? GetValidatedConfigurationSection<T>(this IConfigurationManager configuration,
                                                         string                     configurationSectionKey)
        where T : class, new() =>
        new ServiceCollection()
            .AddOptions()
            .Configure<T>(configuration.GetSection(configurationSectionKey))
            .BuildServiceProvider()
            .GetService<IOptions<T>>()?
            .Value;
}
