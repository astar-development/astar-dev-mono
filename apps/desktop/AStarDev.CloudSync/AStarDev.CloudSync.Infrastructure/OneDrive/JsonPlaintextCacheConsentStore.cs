using System.IO.Abstractions;
using System.Text.Json;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Keeps the plaintext token file consent in a small JSON file. The file exists only while consent is granted.</summary>
/// <param name="consentFile">The JSON file holding the consent.</param>
/// <param name="logger">Reports a consent file that cannot be read or saved, without its path.</param>
public sealed class JsonPlaintextCacheConsentStore(IFileInfo consentFile, ILogger<JsonPlaintextCacheConsentStore> logger) : IPlaintextCacheConsentStore
{
    private const string GrantedValue = "Granted";

    /// <inheritdoc />
    public Option<PlaintextCacheConsent> Load()
    {
        consentFile.Refresh();

        return consentFile.Exists
            ? Try.Run(() => JsonSerializer.Deserialize<StoredConsent>(consentFile.FileSystem.File.ReadAllText(consentFile.FullName))).Match(ToConsent, Unreadable)
            : Option.None<PlaintextCacheConsent>();
    }

    /// <inheritdoc />
    public Result<Unit, ErrorResponse> Save(PlaintextCacheConsent consent) =>
        Try.Run(() => consent is PlaintextCacheConsent.Granted ? Write() : Forget())
            .Match(Result.Success<Unit, ErrorResponse>, NotSaved);

    private static Option<PlaintextCacheConsent> ToConsent(StoredConsent? stored) =>
        stored?.PlaintextCache == GrantedValue ? Option.Some(PlaintextCacheConsentFactory.CreateGranted()) : Option.None<PlaintextCacheConsent>();

    private Option<PlaintextCacheConsent> Unreadable(Exception exception)
    {
        AccountAddLog.PlaintextConsentUnreadable(logger, exception.ErrorType());

        return Option.None<PlaintextCacheConsent>();
    }

    private Unit Write()
    {
        consentFile.Directory?.Create();
        consentFile.FileSystem.File.WriteAllText(consentFile.FullName, JsonSerializer.Serialize(new StoredConsent(GrantedValue)));

        return Unit.Instance;
    }

    private Unit Forget()
    {
        consentFile.Refresh();

        if (consentFile.Exists)
            consentFile.Delete();

        return Unit.Instance;
    }

    private Result<Unit, ErrorResponse> NotSaved(Exception exception)
    {
        AccountAddLog.PlaintextConsentNotSaved(logger, exception.ErrorType());

        return Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The plaintext token file consent could not be saved."));
    }

    private sealed record StoredConsent(string PlaintextCache);
}
