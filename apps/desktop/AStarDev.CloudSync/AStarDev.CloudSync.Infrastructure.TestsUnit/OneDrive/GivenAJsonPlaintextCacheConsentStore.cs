using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Infrastructure.OneDrive;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public class GivenAJsonPlaintextCacheConsentStore
{
    private const string ConsentPath = "/home/user/.config/AStarDev.CloudSync/token-cache-consent.json";

    private readonly RecordingLogger<JsonPlaintextCacheConsentStore> logger = new();

    [Fact]
    public void when_nothing_has_been_saved_then_no_consent_is_remembered() => RememberedAnswer(CreateStore(new MockFileSystem())).ShouldBe("none");

    [Fact]
    public void when_consent_is_granted_then_a_new_store_on_the_same_file_remembers_it()
    {
        var fileSystem = new MockFileSystem();

        var saved = CreateStore(fileSystem).Save(PlaintextCacheConsentFactory.CreateGranted()).Match(_ => true, _ => false);

        (saved, RememberedAnswer(CreateStore(fileSystem))).ShouldBe((true, nameof(PlaintextCacheConsent.Granted)));
    }

    [Fact]
    public void when_consent_is_declined_after_it_was_granted_then_it_is_forgotten_and_the_file_is_removed()
    {
        var fileSystem = new MockFileSystem();
        _ = CreateStore(fileSystem).Save(PlaintextCacheConsentFactory.CreateGranted());

        _ = CreateStore(fileSystem).Save(PlaintextCacheConsentFactory.CreateDeclined());

        (RememberedAnswer(CreateStore(fileSystem)), fileSystem.File.Exists(ConsentPath)).ShouldBe(("none", false));
    }

    [Fact]
    public void when_declined_with_nothing_saved_then_the_save_succeeds() => CreateStore(new MockFileSystem()).Save(PlaintextCacheConsentFactory.CreateDeclined()).Match(_ => true, _ => false).ShouldBeTrue();

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"PlaintextCache":"Maybe"}""")]
    public void when_the_file_is_unreadable_or_holds_an_unknown_value_then_no_consent_is_remembered(string content)
    {
        var fileSystem = new MockFileSystem();
        _ = fileSystem.Directory.CreateDirectory("/home/user/.config/AStarDev.CloudSync");
        fileSystem.File.WriteAllText(ConsentPath, content);

        RememberedAnswer(CreateStore(fileSystem)).ShouldBe("none");
    }

    [Fact]
    public void when_the_file_is_unreadable_then_a_warning_without_the_path_is_logged()
    {
        var fileSystem = new MockFileSystem();
        _ = fileSystem.Directory.CreateDirectory("/home/user/.config/AStarDev.CloudSync");
        fileSystem.File.WriteAllText(ConsentPath, "not json");

        _ = CreateStore(fileSystem).Load();

        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logger.Messages.ShouldAllBe(message => !message.Contains("/home/user"));
        logger.ValuesOf("PlaintextConsentUnreadable")["ErrorType"].ShouldBe(typeof(System.Text.Json.JsonException).FullName);
    }

    [Fact]
    public void when_the_consent_cannot_be_written_then_the_save_fails_and_a_warning_is_logged()
    {
        var fileSystem = new MockFileSystem();
        _ = fileSystem.Directory.CreateDirectory(ConsentPath);

        var saved = CreateStore(fileSystem).Save(PlaintextCacheConsentFactory.CreateGranted()).Match(_ => true, _ => false);

        saved.ShouldBeFalse();
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logger.ValuesOf("PlaintextConsentNotSaved").ShouldContainKey("ErrorType");
    }

    private static string RememberedAnswer(JsonPlaintextCacheConsentStore store) => store.Load().Match(consent => consent.GetType().Name, () => "none");

    private JsonPlaintextCacheConsentStore CreateStore(MockFileSystem fileSystem) => new(fileSystem.FileInfo.New(ConsentPath), logger);
}
