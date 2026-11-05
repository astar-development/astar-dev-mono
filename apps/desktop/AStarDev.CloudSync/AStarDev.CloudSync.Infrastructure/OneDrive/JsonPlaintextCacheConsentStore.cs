using System.IO.Abstractions;
using AStarDev.CloudSync.Core.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Keeps the plaintext token file consent in a small JSON file. The file exists only while consent is granted.</summary>
/// <param name="consentFile">The JSON file holding the consent.</param>
/// <param name="logger">Reports a consent file that cannot be read or saved, without its path.</param>
public sealed class JsonPlaintextCacheConsentStore(IFileInfo consentFile, ILogger<JsonPlaintextCacheConsentStore> logger) : IPlaintextCacheConsentStore
{
    /// <inheritdoc />
    public Option<PlaintextCacheConsent> Load() => throw new NotImplementedException($"{consentFile}{logger}");

    /// <inheritdoc />
    public Result<Unit, ErrorResponse> Save(PlaintextCacheConsent consent) => throw new NotImplementedException();
}
