using System.Security.Cryptography;

namespace AStar.Dev.Utilities.Tests.Unit;

public class GivenEncryptionExtensions
{
    [Fact]
    public void when_a_value_is_encrypted_then_the_result_is_as_expected()
        => "irrelevant-string".Encrypt().ShouldBe("f4eR8dX1bWiwTF/GNd02dTl2wnPfj9jFHWi3ZVCxceQ=");

    [Fact]
    public void when_a_value_is_decrypted_then_the_result_is_as_expected()
        => "f4eR8dX1bWiwTF/GNd02dTl2wnPfj9jFHWi3ZVCxceQ=".Decrypt().ShouldBe("irrelevant-string");

    [Fact]
    public void when_a_value_is_encrypted_and_decrypted_with_a_custom_key_and_iv_then_the_round_trip_returns_the_original_value()
    {
        const string key = "0123456789abcdef0123456789abcdef";
        const string iv  = "0123456789abcdef";

        string encrypted = "irrelevant-string".Encrypt(key, iv);

        encrypted.Decrypt(key, iv).ShouldBe("irrelevant-string");
    }

    [Fact]
    public void when_a_value_is_encrypted_with_an_invalid_key_length_then_throws_cryptographic_exception()
    {
        Action encryptAction = () => "irrelevant-string".Encrypt("too-short-key");

        _ = encryptAction.ShouldThrow<CryptographicException>();
    }

    [Fact]
    public void when_a_value_is_decrypted_with_an_invalid_base_64_string_then_throws_format_exception()
    {
        Action decryptAction = () => "not-valid-base64!!".Decrypt();

        _ = decryptAction.ShouldThrow<FormatException>();
    }
}
