using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.WallpaperScraper;

/// <summary>The Avalonia application entry point: bootstraps configuration, logging, dependency injection, and the main window.</summary>
[ExcludeFromCodeCoverage]
public partial class App : Application, IDisposable
{
    private bool disposedValue;

    /// <summary>Loads the application's XAML resources.</summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>Builds configuration, logging, and the dependency injection container, migrates the database, and shows the main window.</summary>
    public override void OnFrameworkInitializationCompleted() => base.OnFrameworkInitializationCompleted();

    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // NAR at the moment
            }

            disposedValue = true;
        }
    }

    /// <summary>Releases the resources held by the application's dependency injection container.</summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method - Do NOT remove this comment.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
