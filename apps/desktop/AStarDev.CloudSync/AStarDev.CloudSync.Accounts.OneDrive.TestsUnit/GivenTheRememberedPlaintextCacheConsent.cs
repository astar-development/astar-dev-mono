using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class GivenTheRememberedPlaintextCacheConsent : IDisposable
{
    private readonly RecordingLoggerProvider logs = new();
    private readonly ConsentFile consentFile = new();

    [Fact]
    public void when_nothing_has_been_saved_then_no_consent_is_remembered() => RememberedAnswer(CreateModule()).ShouldBe(nameof(PlaintextCacheConsent.Declined));

    [Fact]
    public async Task when_consent_is_granted_then_a_new_module_on_the_same_file_remembers_it()
    {
        _ = await CreateModule(GrantingUser()).AddAsync(TestContext.Current.CancellationToken);

        (consentFile.Exists, RememberedAnswer(CreateModule())).ShouldBe((true, nameof(PlaintextCacheConsent.Granted)));
    }

    [Fact]
    public async Task when_consent_is_granted_then_the_file_holds_the_original_json_format()
    {
        _ = await CreateModule(GrantingUser()).AddAsync(TestContext.Current.CancellationToken);

        consentFile.FileSystem.File.ReadAllText(ConsentFile.Path).ShouldBe(ConsentFile.GrantedJson);
    }

    [Fact]
    public void when_a_file_in_the_original_format_exists_then_the_consent_is_remembered()
    {
        consentFile.Write(ConsentFile.GrantedJson);

        RememberedAnswer(CreateModule()).ShouldBe(nameof(PlaintextCacheConsent.Granted));
    }

    [Fact]
    public async Task when_consent_is_withdrawn_after_it_was_granted_then_it_is_forgotten_and_the_file_is_removed()
    {
        _ = await CreateModule(GrantingUser()).AddAsync(TestContext.Current.CancellationToken);

        _ = await CreateModule().WithdrawAsync();

        (RememberedAnswer(CreateModule()), consentFile.Exists).ShouldBe((nameof(PlaintextCacheConsent.Declined), false));
    }

    [Fact]
    public async Task when_consent_is_withdrawn_with_nothing_saved_then_the_withdrawal_succeeds() => (await CreateModule().WithdrawAsync().MatchAsync(_ => true, _ => false)).ShouldBeTrue();

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"PlaintextCache":"Maybe"}""")]
    public void when_the_file_is_unreadable_or_holds_an_unknown_value_then_no_consent_is_remembered(string content)
    {
        consentFile.Write(content);

        RememberedAnswer(CreateModule()).ShouldBe(nameof(PlaintextCacheConsent.Declined));
    }

    [Fact]
    public void when_the_file_is_unreadable_then_a_warning_without_the_path_is_logged()
    {
        consentFile.Write("not json");

        _ = CreateModule().Current();

        logs.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logs.Logger.Messages.ShouldAllBe(message => !message.Contains("/home/user"));
        logs.Logger.ValuesOf("PlaintextConsentUnreadable")["ErrorType"].ShouldBe(typeof(System.Text.Json.JsonException).FullName);
    }

    [Fact]
    public async Task when_the_consent_cannot_be_written_then_a_warning_is_logged()
    {
        _ = consentFile.FileSystem.Directory.CreateDirectory(ConsentFile.Path);

        _ = await CreateModule(GrantingUser()).AddAsync(TestContext.Current.CancellationToken);

        logs.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logs.Logger.ValuesOf("PlaintextConsentNotSaved").ShouldContainKey("ErrorType");
    }

    private static string RememberedAnswer(OneDriveModule module) => module.Current().GetType().Name;

    private static OneDriveFakes GrantingUser() => new() { Storage = new FakeMsalTokenCacheStorage { SecureStoreFails = true }, Consent = new FakeTokenCacheConsent { Answer = PlaintextCacheConsentFactory.CreateGranted() } };

    private OneDriveModule CreateModule() => CreateModule(new OneDriveFakes());

    private OneDriveModule CreateModule(OneDriveFakes fakes) => new(new OneDriveFakes { Storage = fakes.Storage, Consent = fakes.Consent, ConsentFile = consentFile, Logs = logs });

    public void Dispose()
    {
        logs.Dispose();
    }
}
