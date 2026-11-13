namespace AStarDev.CloudSync.Sync;

/// <summary>What the page loop of an update does next: read another page, finish, or stop with a provider error.</summary>
internal abstract record RemoteUpdateStep
{
    internal static readonly RemoteUpdateStep Next = new Continue();

    internal static readonly RemoteUpdateStep Done = new Finished();

    private RemoteUpdateStep()
    {
    }

    internal static Failed Stop(ProviderError error) => new Failed(error);

    internal sealed record Continue : RemoteUpdateStep;

    internal sealed record Finished : RemoteUpdateStep;

    internal sealed record Failed(ProviderError Error) : RemoteUpdateStep;
}
