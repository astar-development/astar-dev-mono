using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb.Domain;

/// <summary>Identifies a specific item in a OneDrive drive folder.</summary>
public sealed record RemoteItemRef(AccountId AccountId, OneDriveFolderId FolderId, OneDriveItemId RemoteItemId);
