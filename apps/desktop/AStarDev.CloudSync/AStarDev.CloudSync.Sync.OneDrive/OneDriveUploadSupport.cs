using System.Globalization;
using System.Text.Json;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>Shared JSON, tag and target helpers for the OneDrive upload services.</summary>
internal static class OneDriveUploadSupport
{
    internal static Unit Matched(HttpRequestMessage request, string tag)
    {
        OneDriveTransferHttp.AddIfMatch(request, tag);

        return Unit.Instance;
    }

    internal static Option<long> NextOffset(string json) =>
        Try.Run(() => JsonDocument.Parse(json)).Match(
            document => document.RootElement.TryGetProperty("nextExpectedRanges", out var ranges) && ranges.GetArrayLength() > 0 && long.TryParse(ranges[0].GetString()?.Split('-')[0], NumberStyles.None, CultureInfo.InvariantCulture, out var next) ? Option.Some(next) : Option.None<long>(),
            _ => Option.None<long>());

    internal static void WriteFileSystemInfo(Utf8JsonWriter writer, DateTime lastWriteUtc)
    {
        writer.WriteStartObject("fileSystemInfo");
        writer.WriteString("lastModifiedDateTime", OneDriveTransferHttp.Timestamp(lastWriteUtc));
        writer.WriteEndObject();
    }

    internal static Result<UploadedFile, ProviderError> ToUploaded(string json, long fallbackSize, string hash) =>
        Try.Run(() => JsonDocument.Parse(json)).Match(
            document => ToUploaded(document.RootElement, fallbackSize, hash),
            _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected()));

    internal static Result<UploadedFile, ProviderError> ToUploaded(JsonElement item, long fallbackSize, string hash)
    {
        var size = item.TryGetProperty("size", out var length) && length.ValueKind is JsonValueKind.Number ? length.GetInt64() : fallbackSize;

        return RemoteItemIdFactory.Create(TextOf(item, "id")).Match(
            id => ContentTagFactory.Create(TextOf(item, "cTag")).Match(
                cTag => EntityTagFactory.Create(TextOf(item, "eTag")).Match(
                    eTag => ContentHashFactory.Create(hash).Match(contentHash => Result.Success<UploadedFile, ProviderError>(UploadedFileFactory.Create(id, cTag, eTag, size, contentHash)), _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected())),
                    _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected())),
                _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected())),
            _ => Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateUnexpected()));
    }

    internal static Option<string> StringOf(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String ? Option.Some(value.GetString() ?? string.Empty) : Option.None<string>();

    internal static string TextOf(JsonElement item, string name) => StringOf(item, name).Match(text => text, () => string.Empty);

    internal static bool IsLarge(UploadTransfer transfer) => transfer.Length > UploadSessionLimits.PersistedAboveBytes;

    internal static string TargetOf(UploadTransfer transfer) => transfer.Target switch
    {
        UploadTarget.ReplacingItem replacing => $"replace:{replacing.ItemId.Value}:{TagOf(transfer.Precondition).Match(tag => tag, () => string.Empty)}",
        UploadTarget.NewFileIn created => $"new:{created.Parent.Match(parent => parent.Value, () => "root")}/{transfer.Source.Name}",
        _ => "other",
    };

    internal static Option<DateTimeOffset> ExpiryOf(string json) =>
        Try.Run(() => JsonDocument.Parse(json)).Match(document => StringOf(document.RootElement, "expirationDateTime"), _ => Option.None<string>())
            .Match(text => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? Option.Some(parsed) : Option.None<DateTimeOffset>(), () => Option.None<DateTimeOffset>());

    internal static Option<string> TagOf(UploadPrecondition precondition) => precondition is UploadPrecondition.MustMatch match ? Option.Some(match.CTag.Value) : Option.None<string>();
}
