using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeFileAuditor : IFileAuditor
{
    private readonly List<AccountId> audited = [];

    public Result<FileAuditCounts, FileAuditFailure> Outcome { get; set; } = Result.Success<FileAuditCounts, FileAuditFailure>(FileAuditCountsFactory.Create(1_234, 5_678));

    public TaskCompletionSource? Gate { get; set; }

    public Exception? Throws { get; set; }

    public Func<Task>? Delay { get; set; }

    public IReadOnlyList<AccountId> Audited => audited;

    public async Task<Result<FileAuditCounts, FileAuditFailure>> AuditAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        audited.Add(accountId);
        if (Gate is not null)
            await Gate.Task.WaitAsync(cancellationToken);

        if (Delay is not null)
            await Delay().ConfigureAwait(false);

        return Throws is null ? Outcome : throw Throws;
    }
}
