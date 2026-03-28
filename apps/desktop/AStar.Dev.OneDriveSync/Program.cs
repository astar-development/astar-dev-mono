using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.ReactiveUI;
using AStar.Dev.OneDriveSync.Infrastructure.SingleInstance;

namespace AStar.Dev.OneDriveSync;

sealed partial class Program
{
    private const string ApplicationMutexName  = "Global\\AStar.Dev.OneDriveSync.SingleInstance";
    private const string AlreadyRunningMessage = "AStar OneDrive Sync is already running.";
    private const string AlreadyRunningTitle   = "AStar OneDrive Sync";

    [STAThread]
    public static int Main(string[] args)
    {
        using var guard = new SingleInstanceGuard(ApplicationMutexName);

        if (guard.TryAcquire() == SingleInstanceResult.AlreadyRunning)
        {
            ShowAlreadyRunningMessage();

            return 0;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    private static void ShowAlreadyRunningMessage()
    {
        if (OperatingSystem.IsWindows())
            MessageBoxW(nint.Zero, AlreadyRunningMessage, AlreadyRunningTitle, 0x00000040u);
    }

    // P/Invoke required: Avalonia is not yet initialised at this call site,
    // so we cannot show an Avalonia dialog. The native MessageBox is the only
    // option for a pre-Avalonia, Windows-only user notification.
    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    [SupportedOSPlatform("windows")]
    private static partial int MessageBoxW(nint hWnd, string text, string caption, uint type);
}
