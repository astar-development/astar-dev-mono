using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using AStarDev.FunctionalParadigm;
using Microsoft.Win32;

namespace AStarDev.CloudSync.Autostart;

/// <inheritdoc />
[SupportedOSPlatform("windows")]
[ExcludeFromCodeCoverage(Justification = "A thin wrapper over the Windows registry, which CI cannot run. The logic is in WindowsAutostartRegistration.")]
internal sealed class RegistryRunKeyStore : IRunKeyStore
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <inheritdoc />
    public Option<string> Read(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);

        return key?.GetValue(name) is string command ? Option.Some(command) : Option.None<string>();
    }

    /// <inheritdoc />
    public void Write(string name, string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(name, command, RegistryValueKind.String);
    }

    /// <inheritdoc />
    public void Remove(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
