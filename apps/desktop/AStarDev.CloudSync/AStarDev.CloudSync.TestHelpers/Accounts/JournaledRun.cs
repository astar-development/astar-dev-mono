using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed record JournaledRun(SyncRunId Id, AccountId AccountId, SyncTrigger Trigger, DateTimeOffset Started, Option<DateTimeOffset> Finished, Option<SyncRunOutcome> Outcome);
