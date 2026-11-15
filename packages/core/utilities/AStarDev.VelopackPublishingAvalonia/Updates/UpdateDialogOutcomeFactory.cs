namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Creates <see cref="UpdateDialogOutcome"/> cases.</summary>
public static class UpdateDialogOutcomeFactory
{
    /// <summary>Creates the <see cref="Install"/> case.</summary>
    public static UpdateDialogOutcome CreateInstall() => new Install();

    /// <summary>Creates the <see cref="Defer"/> case.</summary>
    public static UpdateDialogOutcome CreateDefer() => new Defer();

    /// <summary>Creates the <see cref="Dismissed"/> case.</summary>
    public static UpdateDialogOutcome CreateDismissed() => new Dismissed();
}
