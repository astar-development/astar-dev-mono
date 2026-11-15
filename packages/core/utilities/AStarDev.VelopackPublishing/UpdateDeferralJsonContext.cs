using System.Text.Json.Serialization;

namespace AStar.Dev.Velopack.Publishing;

[JsonSerializable(typeof(StoredUpdateDeferral))]
internal sealed partial class UpdateDeferralJsonContext : JsonSerializerContext;
