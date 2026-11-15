using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Autostart;

/// <summary>Reads and writes one value of the current user's Windows <c>Run</c> key (#1410). It exists so the registration can be tested without a registry. Each call throws if the key cannot be used.</summary>
public interface IRunKeyStore
{
    /// <summary>Reads the command stored under <paramref name="name"/>, or nothing if there is none.</summary>
    /// <param name="name">The value name.</param>
    Option<string> Read(string name);

    /// <summary>Stores <paramref name="command"/> under <paramref name="name"/>, replacing any earlier value.</summary>
    /// <param name="name">The value name.</param>
    /// <param name="command">The command line to run at login.</param>
    void Write(string name, string command);

    /// <summary>Removes the value. A value that is not there is not an error.</summary>
    /// <param name="name">The value name.</param>
    void Remove(string name);
}
