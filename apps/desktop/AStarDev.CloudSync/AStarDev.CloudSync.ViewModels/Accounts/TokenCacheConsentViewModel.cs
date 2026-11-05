using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Accounts;

/// <summary>Asks, inside the add-account dialog, whether the sign-in tokens may be kept in a plaintext file because the secure store is unavailable.</summary>
/// <param name="dispatcher">Moves the question to the UI thread; it may be asked from a background thread.</param>
/// <param name="localization">Supplies the question text.</param>
public sealed class TokenCacheConsentViewModel(IUiDispatcher dispatcher, ILocalizationService localization) : LocalizedObservableObject(localization), ITokenCacheConsent
{
    /// <summary>Whether the question is showing.</summary>
    public bool IsAsking => throw new NotImplementedException($"{dispatcher}");

    /// <summary>The question's heading.</summary>
    public string TitleText => Localization.GetLocal("TokenCacheConsent.Title");

    /// <summary>The warning that explains the plaintext file.</summary>
    public string MessageText => Localization.GetLocal("TokenCacheConsent.Message");

    /// <summary>The label of the button that agrees.</summary>
    public string AgreeText => Localization.GetLocal("TokenCacheConsent.Agree");

    /// <summary>The label of the button that declines.</summary>
    public string DeclineText => Localization.GetLocal("TokenCacheConsent.Decline");

    /// <summary>Agrees to the plaintext file.</summary>
    public IRelayCommand AgreeCommand => field ??= new RelayCommand(() => throw new NotImplementedException());

    /// <summary>Declines the plaintext file.</summary>
    public IRelayCommand DeclineCommand => field ??= new RelayCommand(() => throw new NotImplementedException());

    /// <inheritdoc />
    public Task<PlaintextCacheConsent> AskAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
