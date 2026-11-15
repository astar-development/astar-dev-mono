using System.IO.Abstractions;
using System.Text.Json;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Velopack.Publishing;

/// <summary>Keeps the update deferral in a small JSON file.</summary>
/// <param name="deferralFile">The JSON file holding the deferral.</param>
/// <param name="logger">Reports a deferral that cannot be read or saved.</param>
public sealed class JsonUpdateDeferralStore(IFileInfo deferralFile, ILogger<JsonUpdateDeferralStore> logger) : IUpdateDeferralStore
{
    /// <inheritdoc />
    public UpdateDeferral Load()
    {
        deferralFile.Refresh();

        return deferralFile.Exists ? Try.Run(Read).Match(deferral => deferral, Unreadable) : UpdateDeferralFactory.CreateNotDeferred();
    }

    /// <inheritdoc />
    public void Save(UpdateDeferral deferral) => _ = Try.Run(() => Write(deferral)).Match(written => written, NotSaved);

    private UpdateDeferral Read()
    {
        var stored = JsonSerializer.Deserialize(deferralFile.FileSystem.File.ReadAllText(deferralFile.FullName), UpdateDeferralJsonContext.Default.StoredUpdateDeferral);

        return stored is null ? UpdateDeferralFactory.CreateNotDeferred() : UpdateDeferralFactory.CreateDeferredSince(stored.Version, stored.FirstDeferredAt, stored.LastPromptedAt);
    }

    private Unit Write(UpdateDeferral deferral)
    {
        if (deferral is not DeferredSince deferred)
            return Remove();

        deferralFile.Directory?.Create();
        deferralFile.FileSystem.File.WriteAllText(deferralFile.FullName, JsonSerializer.Serialize(new StoredUpdateDeferral(deferred.Version, deferred.FirstDeferredAt, deferred.LastPromptedAt), UpdateDeferralJsonContext.Default.StoredUpdateDeferral));

        return Unit.Instance;
    }

    private Unit Remove()
    {
        deferralFile.Refresh();
        if (deferralFile.Exists)
            deferralFile.Delete();

        return Unit.Instance;
    }

    private UpdateDeferral Unreadable(Exception exception)
    {
        VelopackUpdateLog.DeferralUnreadable(logger, exception.ErrorType());

        return UpdateDeferralFactory.CreateNotDeferred();
    }

    private Unit NotSaved(Exception exception)
    {
        VelopackUpdateLog.DeferralNotSaved(logger, exception.ErrorType());

        return Unit.Instance;
    }
}
