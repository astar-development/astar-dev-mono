namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Creates <see cref="QuitChoice"/> values.</summary>
public static class QuitChoiceFactory
{
    /// <summary>Creates the <see cref="QuitChoice.QuitAnyway"/> case.</summary>
    public static QuitChoice CreateQuitAnyway() => new QuitChoice.QuitAnyway();

    /// <summary>Creates the <see cref="QuitChoice.StayOpen"/> case.</summary>
    public static QuitChoice CreateStayOpen() => new QuitChoice.StayOpen();
}
