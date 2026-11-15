namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>How the user left the update dialog (#1412).</summary>
public abstract record UpdateDialogOutcome
{
    private protected UpdateDialogOutcome()
    {
    }
}

/// <summary>The user chose to restart now and install the update.</summary>
public sealed record Install : UpdateDialogOutcome
{
    internal Install()
    {
    }
}

/// <summary>The user chose to install later.</summary>
public sealed record Defer : UpdateDialogOutcome
{
    internal Defer()
    {
    }
}

/// <summary>The user closed the dialog without choosing, or nothing has been chosen yet.</summary>
public sealed record Dismissed : UpdateDialogOutcome
{
    internal Dismissed()
    {
    }
}
