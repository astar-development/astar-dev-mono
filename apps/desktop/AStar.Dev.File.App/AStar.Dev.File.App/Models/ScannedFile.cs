using System;

namespace AStar.Dev.File.App.Models;

public class ScannedFile
{
    public int Id { get; set; }

    public required string RootPath { get; set; } = string.Empty;

    public required string FolderPath { get; set; } = string.Empty;

    public required string FileName { get; set; } = string.Empty;

    public required string FullPath { get; set; } = string.Empty;

    public DateTime LastModified { get; set; }

    public long SizeInBytes { get; set; }

    public FileType FileType { get; set; }

    public DateTime? LastViewed { get; set; }

    public bool PendingDelete { get; set; }

    public DateTime LastScannedAt { get; set; }
}
