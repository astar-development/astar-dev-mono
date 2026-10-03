namespace AStarDev.ControlDb.FileDetail;

public readonly record struct FileId(Guid Value);
public readonly record struct ImageId(Guid Value);
public readonly record struct FileAccessDetailId(Guid Value);
public readonly record struct DeletionStatusId(Guid Value);
public readonly record struct FileHandle(string Value);
public readonly record struct FileName(string Value);
public readonly record struct DirectoryName(string Value);
