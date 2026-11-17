using System.IO.Abstractions;
using System.Security.Cryptography;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>The state of one upload while it runs.</summary>
internal sealed class UploadTransfer(AccountId accountId, UploadTarget target, IFileInfo source, UploadPrecondition precondition, IProgress<TransferProgress> progress)
{
    internal AccountId AccountId { get; } = accountId;

    internal UploadTarget Target { get; } = target;

    internal IFileInfo Source { get; } = source;

    internal UploadPrecondition Precondition { get; } = precondition;

    internal IProgress<TransferProgress> Progress { get; } = progress;

    internal Option<Uri> Session { get; set; } = Option.None<Uri>();

    internal Option<DateTimeOffset> ExpiresAt { get; set; } = Option.None<DateTimeOffset>();

    internal long Length { get; set; }

    internal DateTime LastWriteUtc { get; set; }

    internal long Offset { get; set; }

    internal long HashedUpTo { get; set; }

    internal IncrementalHash Hash { get; } = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    internal int Fragments { get; set; }

    internal int Attempts { get; set; }

    internal int Restarts { get; set; }
}
