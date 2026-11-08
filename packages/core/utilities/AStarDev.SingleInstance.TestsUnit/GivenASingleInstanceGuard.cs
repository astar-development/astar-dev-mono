using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.SingleInstance.TestsUnit;

public sealed class GivenASingleInstanceGuard : IDisposable
{
    private readonly TemporaryDataFolder data = new();
    private readonly RecordingLogger logger = new();
    private readonly ServiceProvider provider;

    public GivenASingleInstanceGuard() => provider = new ServiceCollection()
        .AddLogging(logging => logging.ClearProviders().AddProvider(logger))
        .AddSingleInstance()
        .BuildServiceProvider();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private ISingleInstanceGuard Guard => provider.GetRequiredService<ISingleInstanceGuard>();

    [Fact]
    public async Task when_no_instance_is_running_then_the_claim_is_primary()
    {
        var claim = await Guard.ClaimAsync(data.Options(), CancellationToken);

        await using var primary = claim.ShouldBeOfType<InstanceClaim.Primary>().Instance;
    }

    [Fact]
    public async Task when_an_instance_is_running_then_a_second_launch_hands_off_and_the_running_instance_is_asked_to_show_itself()
    {
        await using var primary = (await Guard.ClaimAsync(data.Options(), CancellationToken)).ShouldBeOfType<InstanceClaim.Primary>().Instance;
        var activations = 0;
        primary.ActivationRequested += (_, _) => Interlocked.Increment(ref activations);

        var second = await Guard.ClaimAsync(data.Options(), CancellationToken);

        second.ShouldBe(InstanceClaimFactory.CreateHandedOff());
        activations.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_running_instance_is_disposed_then_the_next_launch_is_primary()
    {
        var first = (await Guard.ClaimAsync(data.Options(), CancellationToken)).ShouldBeOfType<InstanceClaim.Primary>().Instance;
        await first.DisposeAsync();

        var next = await Guard.ClaimAsync(data.Options(), CancellationToken);

        await using var primary = next.ShouldBeOfType<InstanceClaim.Primary>().Instance;
    }

    [Fact]
    public async Task when_a_crashed_instance_left_its_socket_file_behind_then_the_next_launch_is_primary_and_can_be_handed_off_to()
    {
        data.Folder.Create();
        await data.Folder.FileSystem.File.WriteAllTextAsync(data.SocketPath, "stale", CancellationToken);
        await using var primary = (await Guard.ClaimAsync(data.Options(), CancellationToken)).ShouldBeOfType<InstanceClaim.Primary>().Instance;

        var second = await Guard.ClaimAsync(data.Options(), CancellationToken);

        second.ShouldBe(InstanceClaimFactory.CreateHandedOff());
    }

    [Fact]
    public async Task when_the_lock_is_held_but_nothing_listens_then_the_launch_is_not_answered_and_a_warning_is_logged()
    {
        data.Folder.Create();
        await using var heldLock = new FileStream(data.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        var claim = await Guard.ClaimAsync(data.Options(), CancellationToken);

        claim.ShouldBe(InstanceClaimFactory.CreateNotAnswered());
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task when_the_running_instance_accepts_but_never_answers_then_the_launch_is_not_answered_after_the_timeout()
    {
        data.Folder.Create();
        await using var heldLock = new FileStream(data.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var silentListener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        silentListener.Bind(new UnixDomainSocketEndPoint(data.SocketPath));
        silentListener.Listen();

        var claim = await Guard.ClaimAsync(data.Options(TimeSpan.FromMilliseconds(300)), CancellationToken);

        claim.ShouldBe(InstanceClaimFactory.CreateNotAnswered());
    }

    [Fact]
    public async Task when_two_users_have_different_data_folders_then_both_are_primary()
    {
        using var otherUser = new TemporaryDataFolder();
        await using var first = (await Guard.ClaimAsync(data.Options(), CancellationToken)).ShouldBeOfType<InstanceClaim.Primary>().Instance;

        var second = await Guard.ClaimAsync(otherUser.Options(), CancellationToken);

        await using var secondPrimary = second.ShouldBeOfType<InstanceClaim.Primary>().Instance;
    }

    [Fact]
    public async Task when_many_launches_race_then_exactly_one_is_primary()
    {
        var claims = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => Guard.ClaimAsync(data.Options(TimeSpan.FromSeconds(2)), CancellationToken), CancellationToken)));
        var primaries = claims.OfType<InstanceClaim.Primary>().ToList();

        primaries.Count.ShouldBe(1);
        claims.Where(claim => claim is not InstanceClaim.Primary).ShouldAllBe(claim => claim is InstanceClaim.HandedOff || claim is InstanceClaim.NotAnswered);
        await primaries[0].Instance.DisposeAsync();
    }

    [Fact]
    public async Task when_the_data_folder_cannot_be_created_then_the_lock_is_unavailable_and_an_error_is_logged()
    {
        data.Folder.Parent!.Create();
        await data.Folder.FileSystem.File.WriteAllTextAsync(data.Folder.FullName, "a file where the folder should be", CancellationToken);

        var claim = await Guard.ClaimAsync(data.Options(), CancellationToken);

        claim.ShouldBe(InstanceClaimFactory.CreateLockUnavailable());
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task when_the_lock_file_name_is_too_long_for_the_file_system_then_the_lock_is_unavailable()
    {
        var options = SingleInstanceOptionsFactory.Create(data.Folder, new string('n', 300), TimeSpan.FromMilliseconds(300));

        var claim = await Guard.ClaimAsync(options, CancellationToken);

        claim.ShouldBe(InstanceClaimFactory.CreateLockUnavailable());
    }

    [Fact]
    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    public async Task when_the_data_folder_is_read_only_then_the_lock_is_unavailable()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows() || Environment.UserName == "root", "Unix permissions only, and root ignores them.");
        data.Folder.Create();
        File.SetUnixFileMode(data.Folder.FullName, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        var claim = await Guard.ClaimAsync(data.Options(), CancellationToken);

        File.SetUnixFileMode(data.Folder.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        claim.ShouldBe(InstanceClaimFactory.CreateLockUnavailable());
    }

    [Fact]
    public async Task when_the_socket_path_is_too_long_then_the_instance_is_still_primary_and_a_warning_is_logged()
    {
        var options = SingleInstanceOptionsFactory.Create(data.Folder, new string('s', 120), TimeSpan.FromMilliseconds(300));

        var claim = await Guard.ClaimAsync(options, CancellationToken);

        await using var primary = claim.ShouldBeOfType<InstanceClaim.Primary>().Instance;
        logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task when_a_client_sends_something_other_than_show_then_the_running_instance_is_not_activated_and_keeps_answering()
    {
        await using var primary = (await Guard.ClaimAsync(data.Options(), CancellationToken)).ShouldBeOfType<InstanceClaim.Primary>().Instance;
        var activations = 0;
        primary.ActivationRequested += (_, _) => Interlocked.Increment(ref activations);
        using (var stranger = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
        {
            await stranger.ConnectAsync(new UnixDomainSocketEndPoint(data.SocketPath), CancellationToken);
            _ = await stranger.SendAsync("hello\n"u8.ToArray(), SocketFlags.None, CancellationToken);
        }

        var second = await Guard.ClaimAsync(data.Options(), CancellationToken);

        (second, activations).ShouldBe((InstanceClaimFactory.CreateHandedOff(), 1));
    }

    [Fact]
    public async Task when_an_activation_handler_throws_then_the_second_launch_is_still_handed_off()
    {
        await using var primary = (await Guard.ClaimAsync(data.Options(), CancellationToken)).ShouldBeOfType<InstanceClaim.Primary>().Instance;
        primary.ActivationRequested += (_, _) => throw new InvalidOperationException("The window is gone.");

        var second = await Guard.ClaimAsync(data.Options(), CancellationToken);

        second.ShouldBe(InstanceClaimFactory.CreateHandedOff());
    }

    [Fact]
    public async Task when_the_caller_cancels_during_the_hand_off_then_the_claim_is_cancelled()
    {
        data.Folder.Create();
        await using var heldLock = new FileStream(data.LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var silentListener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        silentListener.Bind(new UnixDomainSocketEndPoint(data.SocketPath));
        silentListener.Listen();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Should.ThrowAsync<OperationCanceledException>(() => Guard.ClaimAsync(data.Options(TimeSpan.FromSeconds(5)), cancellation.Token));
    }

    [Fact]
    public async Task when_the_caller_cancels_then_the_claim_is_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => Guard.ClaimAsync(data.Options(), cancellation.Token));
    }

    [Fact]
    public async Task when_the_data_folder_path_holds_personal_data_then_no_log_contains_it()
    {
        using var personal = new TemporaryDataFolder("jane.doe@example.com");
        await using var primary = (await Guard.ClaimAsync(personal.Options(), CancellationToken)).ShouldBeOfType<InstanceClaim.Primary>().Instance;
        _ = await Guard.ClaimAsync(personal.Options(), CancellationToken);

        logger.Entries.ShouldNotBeEmpty();
        logger.Entries.ShouldAllBe(entry => !entry.Message.Contains("jane.doe", StringComparison.OrdinalIgnoreCase) && !entry.Message.Contains(personal.Folder.FullName, StringComparison.Ordinal));
    }

    public void Dispose()
    {
        provider.Dispose();
        logger.Dispose();
        data.Dispose();
    }
}
