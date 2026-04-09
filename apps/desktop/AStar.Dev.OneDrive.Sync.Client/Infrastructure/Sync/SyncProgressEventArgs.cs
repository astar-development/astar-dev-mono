namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public sealed class SyncProgressEventArgs(string accountId, string folderId, int completed, int total, string currentFile, SyncState syncState) : EventArgs
{
    public string AccountId { get; } = accountId;
    public string FolderId { get; } = folderId;
    public int Completed { get; } = completed;
    public int Total { get; } = total;
    public string CurrentFile { get; } = currentFile;
    public double Fraction => Total > 0 ? (double)Completed / Total : 0;
    public SyncState SyncState { get; } = syncState;
}
