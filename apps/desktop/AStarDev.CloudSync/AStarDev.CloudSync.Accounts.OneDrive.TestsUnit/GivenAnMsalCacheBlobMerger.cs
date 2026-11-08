using System.Text;
using System.Text.Json.Nodes;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public class GivenAnMsalCacheBlobMerger
{
    private static readonly byte[] KeyringWithAccountA = Blob("""{"Account":{"a-key":{"username":"a"}},"RefreshToken":{"a-rt":{"secret":"a-old"}}}""");
    private static readonly byte[] PlaintextWithAccountB = Blob("""{"Account":{"b-key":{"username":"b"}},"RefreshToken":{"b-rt":{"secret":"b"}}}""");

    [Fact]
    public void when_the_keyring_is_empty_then_the_plaintext_cache_is_kept_whole()
    {
        var merged = Parse(MsalCacheBlobMerger.Merge([], PlaintextWithAccountB));

        merged["Account"]!.AsObject().Select(entry => entry.Key).ShouldBe(["b-key"]);
    }

    [Fact]
    public void when_both_hold_different_accounts_then_the_merge_keeps_both()
    {
        var merged = Parse(MsalCacheBlobMerger.Merge(KeyringWithAccountA, PlaintextWithAccountB));

        merged["Account"]!.AsObject().Select(entry => entry.Key).Order().ShouldBe(["a-key", "b-key"]);
        merged["RefreshToken"]!.AsObject().Select(entry => entry.Key).Order().ShouldBe(["a-rt", "b-rt"]);
    }

    [Fact]
    public void when_both_hold_the_same_entry_then_the_plaintext_entry_wins()
    {
        var newerPlaintext = Blob("""{"RefreshToken":{"a-rt":{"secret":"a-new"}}}""");

        var merged = Parse(MsalCacheBlobMerger.Merge(KeyringWithAccountA, newerPlaintext));

        merged["RefreshToken"]!["a-rt"]!["secret"]!.GetValue<string>().ShouldBe("a-new");
    }

    [Fact]
    public void when_the_plaintext_cache_is_empty_then_the_keyring_cache_is_unchanged()
    {
        var merged = Parse(MsalCacheBlobMerger.Merge(KeyringWithAccountA, []));

        merged["Account"]!.AsObject().Select(entry => entry.Key).ShouldBe(["a-key"]);
    }

    private static byte[] Blob(string json) => Encoding.UTF8.GetBytes(json);

    private static JsonObject Parse(byte[] blob) => JsonNode.Parse(blob)!.AsObject();
}
