using System.Text.Json.Serialization;

namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>Source-generated JSON metadata for the display preferences file, so serialising it is trim and AOT safe.</summary>
[JsonSerializable(typeof(JsonDisplayPreferencesStore.StoredPreferences))]
internal sealed partial class DisplayPreferencesJsonContext : JsonSerializerContext;
