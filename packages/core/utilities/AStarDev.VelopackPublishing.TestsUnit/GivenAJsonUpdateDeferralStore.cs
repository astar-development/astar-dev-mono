using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Velopack.Publishing.TestsUnit;

public sealed class GivenAJsonUpdateDeferralStore
{
    private const string DeferralPath = "/home/user/.config/app/update-deferral.json";
    private static readonly DateTimeOffset First = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Last = First.AddHours(2);

    private static JsonUpdateDeferralStore CreateStore(MockFileSystem fileSystem) => new(fileSystem.FileInfo.New(DeferralPath), NullLogger<JsonUpdateDeferralStore>.Instance);

    [Fact]
    public void when_nothing_has_been_saved_then_nothing_is_deferred()
    {
        var deferral = CreateStore(new MockFileSystem()).Load();

        deferral.ShouldBeOfType<NotDeferred>();
    }

    [Fact]
    public void when_a_deferral_is_saved_then_a_new_store_on_the_same_file_loads_it()
    {
        var fileSystem = new MockFileSystem();

        CreateStore(fileSystem).Save(UpdateDeferralFactory.CreateDeferredSince("1.2.0", First, Last));
        var deferred = CreateStore(fileSystem).Load().ShouldBeOfType<DeferredSince>();

        deferred.Version.ShouldBe("1.2.0");
        deferred.FirstDeferredAt.ShouldBe(First);
        deferred.LastPromptedAt.ShouldBe(Last);
    }

    [Fact]
    public void when_nothing_is_deferred_any_more_then_the_saved_file_is_removed()
    {
        var fileSystem = new MockFileSystem();
        var store = CreateStore(fileSystem);
        store.Save(UpdateDeferralFactory.CreateDeferredSince("1.2.0", First, Last));

        store.Save(UpdateDeferralFactory.CreateNotDeferred());

        fileSystem.File.Exists(DeferralPath).ShouldBeFalse();
        store.Load().ShouldBeOfType<NotDeferred>();
    }

    [Fact]
    public void when_nothing_is_deferred_and_no_file_exists_then_saving_does_nothing()
    {
        var fileSystem = new MockFileSystem();

        CreateStore(fileSystem).Save(UpdateDeferralFactory.CreateNotDeferred());

        fileSystem.File.Exists(DeferralPath).ShouldBeFalse();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public void when_the_saved_file_is_unreadable_then_nothing_is_deferred(string content)
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/home/user/.config/app");
        fileSystem.File.WriteAllText(DeferralPath, content);

        var deferral = CreateStore(fileSystem).Load();

        deferral.ShouldBeOfType<NotDeferred>();
    }

    [Fact]
    public void when_the_deferral_cannot_be_saved_then_saving_does_not_throw()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory(DeferralPath);

        Should.NotThrow(() => CreateStore(fileSystem).Save(UpdateDeferralFactory.CreateDeferredSince("1.2.0", First, Last)));
    }

    [Fact]
    public void when_registered_then_the_store_is_resolvable_and_uses_the_given_file()
    {
        var fileSystem = new MockFileSystem();
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddVelopackUpdateDeferral(_ => fileSystem.FileInfo.New(DeferralPath));
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IUpdateDeferralStore>().Save(UpdateDeferralFactory.CreateDeferredSince("1.2.0", First, Last));

        fileSystem.File.Exists(DeferralPath).ShouldBeTrue();
    }
}
