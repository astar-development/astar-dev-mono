using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>
///     Registers a view together with a <see cref="Func{TView}" /> factory so windows can create fresh,
///     fully-composed view instances on demand.
/// </summary>
public static class ViewFactoryServiceCollectionExtensions
{
    /// <summary>
    ///     Registers <typeparamref name="TView" /> as transient and a transient <see cref="Func{TView}" />
    ///     that resolves a new instance on every invocation.
    /// </summary>
    public static IServiceCollection AddViewFactory<TView>(this IServiceCollection services) where TView : class
        => services
            .AddTransient<TView>()
            .AddTransient<Func<TView>>(sp => sp.GetRequiredService<TView>);
}
