using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public sealed class WindowsRunKey : IWindowsRunKey
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <inheritdoc />
    public void SetValue(string valueName, string command)
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key.SetValue(valueName, command);
    }

    /// <inheritdoc />
    public void DeleteValue(string valueName)
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }
}
