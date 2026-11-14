using System.Text.Json.Serialization;

namespace AStarDev.CloudSync.Accounts.OneDrive;

/// <summary>Source-generated JSON metadata for the plaintext token file consent file, so serialising it is trim and AOT safe.</summary>
[JsonSerializable(typeof(JsonPlaintextCacheConsentStore.StoredConsent))]
internal sealed partial class PlaintextCacheConsentJsonContext : JsonSerializerContext;
