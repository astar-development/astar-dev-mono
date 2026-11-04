using AStarDev.CloudSync.Core;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>Cloud storage usage as shown in a storage bar.</summary>
/// <param name="storage">The storage usage.</param>
/// <param name="localization">Supplies the size text.</param>
public sealed class StorageViewModel(StorageUsage storage, ILocalizationService localization) : LocalizedObservableObject(localization)
{
    /// <summary>Used of total, e.g. "728.9 GB of 1.0 TB".</summary>
    public string Text => Localization.GetLocal("Storage.Usage", ByteSizeFormatter.Format(storage.UsedBytes, Localization), ByteSizeFormatter.Format(storage.TotalBytes, Localization));

    /// <summary>The used fraction, from 0 to 1.</summary>
    public double Fraction => storage.TotalBytes == 0 ? 0 : Math.Min((double)storage.UsedBytes / storage.TotalBytes, 1);
}
