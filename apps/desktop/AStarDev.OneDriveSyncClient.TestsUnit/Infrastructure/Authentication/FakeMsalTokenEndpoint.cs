using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Identity.Client;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Authentication;

internal sealed class FakeMsalTokenEndpoint(IReadOnlyDictionary<string, string> idTokenClaims) : IMsalHttpClientFactory
{
    public const string ObjectId = "00000000-0000-0000-0000-0000000000aa";
    public const string TenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";
    public const string ClientId = "test-client-id";
    public const string HomeAccountId = $"{ObjectId}.{TenantId}";

    public HttpClient GetHttpClient() => new(new TokenResponseHandler(idTokenClaims));

    private static string Base64Url(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string IdToken(IReadOnlyDictionary<string, string> claims)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = new Dictionary<string, object>
        {
            ["aud"] = ClientId,
            ["iss"] = $"https://login.microsoftonline.com/{TenantId}/v2.0",
            ["iat"] = now,
            ["nbf"] = now,
            ["exp"] = now + 3600,
            ["oid"] = ObjectId,
            ["sub"] = "subject",
            ["tid"] = TenantId,
            ["ver"] = "2.0"
        };

        foreach (var claim in claims)
            payload[claim.Key] = claim.Value;

        return $"{Base64Url("""{"alg":"none","typ":"JWT"}""")}.{Base64Url(JsonSerializer.Serialize(payload))}.";
    }

    private sealed class TokenResponseHandler(IReadOnlyDictionary<string, string> idTokenClaims) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, object>
            {
                ["token_type"] = "Bearer",
                ["scope"] = "Files.ReadWrite User.Read",
                ["expires_in"] = 3600,
                ["ext_expires_in"] = 3600,
                ["access_token"] = "fake-access-token",
                ["refresh_token"] = "fake-refresh-token",
                ["id_token"] = IdToken(idTokenClaims),
                ["client_info"] = Base64Url(JsonSerializer.Serialize(new Dictionary<string, string> { ["uid"] = ObjectId, ["utid"] = TenantId }))
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") });
        }
    }
}
