using System.Globalization;
using AStarDev.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.Startup;

public static class LocalizationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddLocalizationServices() => services.AddLocalizationService(typeof(LocalizationExtensions).Assembly, new CultureInfo("en-GB"));
    }
}
