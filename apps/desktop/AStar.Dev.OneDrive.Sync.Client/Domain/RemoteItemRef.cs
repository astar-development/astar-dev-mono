namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Identifies a specific item in a OneDrive drive folder.</summary>
public sealed record RemoteItemRef(AccountId AccountId, OneDriveFolderId FolderId, OneDriveItemId RemoteItemId);
