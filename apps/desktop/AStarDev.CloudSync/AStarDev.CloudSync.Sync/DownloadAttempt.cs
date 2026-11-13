using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

internal sealed class DownloadAttempt(AccountId accountId, DownloadPlanItem.DownloadFile plan, IFileInfo target, IFileInfo temp, long size, Option<SyncBaselineEntry> baseline)
{
    internal AccountId AccountId { get; } = accountId;

    internal DownloadPlanItem.DownloadFile Plan { get; } = plan;

    internal IFileInfo Target { get; } = target;

    internal IFileInfo Temp { get; } = temp;

    internal long Size { get; } = size;

    internal Option<SyncBaselineEntry> Baseline { get; } = baseline;
}
