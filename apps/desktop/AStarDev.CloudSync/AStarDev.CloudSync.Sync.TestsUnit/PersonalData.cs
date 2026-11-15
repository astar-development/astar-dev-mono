namespace AStarDev.CloudSync.Sync.TestsUnit;

internal static class PersonalData
{
    internal const string Email = "jane.doe@contoso.com";
    internal const string DisplayName = "Jane Doe";
    internal const string DownloadUrl = "https://contoso-my.sharepoint.com/personal/jane_doe/_layouts/15/download.aspx?tempauth=SecretDownloadTempAuth";
    internal const string UploadUrl = "https://upload.contoso.example/sessions/SecretUploadSession?tempauth=SecretUploadTempAuth";
    internal const string BearerToken = "Bearer SecretBearerJwt";
    internal const string AccessToken = "SecretAccessTokenValue";
    internal const string RefreshToken = "SecretRefreshTokenValue";

    internal static readonly IReadOnlyList<string> All =
    [
        Email,
        DisplayName,
        "passport-scan",
        "SecretClientFolder",
        "/sync/root",
        "Docs",
        DownloadUrl,
        UploadUrl,
        "SecretDownloadTempAuth",
        "SecretUploadSession",
        "SecretUploadTempAuth",
        "contoso-my.sharepoint.com",
        "upload.contoso.example",
        BearerToken,
        "SecretBearerJwt",
        AccessToken,
        RefreshToken,
    ];
}
