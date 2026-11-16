using System.Runtime.CompilerServices;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification.Sqlite;

/// <summary>Streams the files of every account's sync baseline through the public baseline reader, one page at a time, so the whole baseline is never held in memory. A page that cannot be read ends the stream with an <see cref="InvalidOperationException"/>, because a silent end would look like a complete run.</summary>
/// <param name="accountStore">Lists the accounts.</param>
/// <param name="baselineReader">Reads a page of an account's baseline.</param>
/// <param name="logger">Reports a read that fails.</param>
internal sealed class SqliteClassifiableFileSource(IAccountStore accountStore, ISyncBaselineReader baselineReader, ILogger<SqliteClassifiableFileSource> logger) : IClassifiableFileSource
{
    private const int PageSize = 5000;

    /// <inheritdoc />
    public async IAsyncEnumerable<ClassifiableFile> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var accounts = await accountStore.GetAllAsync(cancellationToken).MatchAsync(stored => stored, _ => throw ReadFailed()).ConfigureAwait(false);
        foreach (var account in accounts)
        {
            var after = Option.None<RelativeSyncPath>();
            IReadOnlyList<SyncBaselineEntry> page;
            do
            {
                page = await baselineReader.GetUnderAsync(account.Id, default, after, PageSize, cancellationToken).MatchAsync(entries => entries, _ => throw ReadFailed()).ConfigureAwait(false);
                foreach (var entry in page.Where(entry => entry.Kind is SyncBaselineKind.File))
                    yield return ClassifiableFileFactory.Create(entry.AccountId, entry.Path);

                after = page.Count > 0 ? Option.Some(page[^1].Path) : after;
            }
            while (page.Count == PageSize);
        }
    }

    private InvalidOperationException ReadFailed()
    {
        LogMessage.Error(logger, "The sync baseline could not be read for classification.");

        return new InvalidOperationException("The sync baseline could not be read.");
    }
}
