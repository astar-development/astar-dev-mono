using System;
using AStar.Dev.Conflict.Resolution.Domain;

namespace AStar.Dev.OneDriveSync.Tests.Integration.Helpers;

internal sealed class ConflictRecordBuilder
{
    private Guid _accountId = Guid.NewGuid();
    private string _filePath = "/files/document.txt";
    private DateTimeOffset _localLastModified = DateTimeOffset.UtcNow.AddMinutes(-10);
    private DateTimeOffset _remoteLastModified = DateTimeOffset.UtcNow;
    private ConflictType _conflictType = ConflictType.BothModified;

    public ConflictRecordBuilder WithAccountId(Guid accountId)
    {
        _accountId = accountId;

        return this;
    }

    public ConflictRecordBuilder WithFilePath(string filePath)
    {
        _filePath = filePath;

        return this;
    }

    public ConflictRecordBuilder WithConflictType(ConflictType conflictType)
    {
        _conflictType = conflictType;

        return this;
    }

    public ConflictRecord Build()
        => ConflictRecordFactory.Create(_accountId, _filePath, _localLastModified, _remoteLastModified, _conflictType);
}
