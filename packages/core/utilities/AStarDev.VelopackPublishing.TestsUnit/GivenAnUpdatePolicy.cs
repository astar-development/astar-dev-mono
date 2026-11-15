using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace AStar.Dev.Velopack.Publishing.TestsUnit;

public sealed class GivenAnUpdatePolicy
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly IUpdatePolicy Policy = new ServiceCollection()
        .AddLogging()
        .AddVelopackUpdates(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Updates:GithubRepositoryUrl"] = "https://github.com/astar-development/astar-dev-mono", ["Updates:ChannelPrefix"] = "test" }).Build())
        .BuildServiceProvider()
        .GetRequiredService<IUpdatePolicy>();

    private static UpdateCheckResult Found(string version = "1.2.3") => UpdateCheckResultFactory.CreateFound(new UpdateInfo(new VelopackAsset { Version = SemanticVersion.Parse(version) }, false, null, []));

    private static UpdateAction Decide(UpdateCheckResult result, UpdateDeferral deferral) => Policy.Decide(result, deferral, Now);

    [Fact]
    public void when_an_update_is_found_and_never_deferred_then_the_user_is_asked()
    {
        Decide(Found(), UpdateDeferralFactory.CreateNotDeferred()).ShouldBe(UpdateActionFactory.CreatePrompt());
    }

    [Fact]
    public void when_the_user_deferred_less_than_an_hour_ago_then_the_reminder_is_one_hour_after_the_last_question()
    {
        var asked = Now.AddMinutes(-20);

        Decide(Found(), UpdateDeferralFactory.CreateDeferredSince("1.2.3", Now.AddHours(-2).AddMinutes(-20), asked)).ShouldBe(UpdateActionFactory.CreateRemindLater(asked.AddHours(1)));
    }

    [Fact]
    public void when_the_last_question_was_exactly_an_hour_ago_then_the_user_is_asked_again()
    {
        Decide(Found(), UpdateDeferralFactory.CreateDeferredSince("1.2.3", Now.AddHours(-3), Now.AddHours(-1))).ShouldBe(UpdateActionFactory.CreatePrompt());
    }

    [Fact]
    public void when_the_user_deferred_more_than_an_hour_ago_then_the_user_is_asked_again()
    {
        Decide(Found(), UpdateDeferralFactory.CreateDeferredSince("1.2.3", Now.AddDays(-2), Now.AddHours(-5))).ShouldBe(UpdateActionFactory.CreatePrompt());
    }

    [Fact]
    public void when_the_update_has_been_deferred_for_seven_days_then_it_is_forced_after_a_warning()
    {
        Decide(Found(), UpdateDeferralFactory.CreateDeferredSince("1.2.3", Now.AddDays(-7), Now.AddMinutes(-5))).ShouldBe(UpdateActionFactory.CreateForceAfterWarning());
    }

    [Fact]
    public void when_the_update_has_been_deferred_for_just_under_seven_days_then_it_is_not_forced()
    {
        Decide(Found(), UpdateDeferralFactory.CreateDeferredSince("1.2.3", Now.AddDays(-7).AddSeconds(1), Now.AddHours(-2))).ShouldBe(UpdateActionFactory.CreatePrompt());
    }

    [Fact]
    public void when_a_newer_version_is_found_than_the_one_deferred_then_the_user_is_asked_and_the_clock_starts_again()
    {
        Decide(Found("1.3.0"), UpdateDeferralFactory.CreateDeferredSince("1.2.3", Now.AddDays(-30), Now.AddMinutes(-1))).ShouldBe(UpdateActionFactory.CreatePrompt());
    }

    [Fact]
    public void when_the_last_question_is_in_the_future_then_the_reminder_is_still_an_hour_after_it()
    {
        var asked = Now.AddMinutes(10);

        Decide(Found(), UpdateDeferralFactory.CreateDeferredSince("1.2.3", Now.AddHours(-1), asked)).ShouldBe(UpdateActionFactory.CreateRemindLater(asked.AddHours(1)));
    }

    [Theory]
    [InlineData("uptodate")]
    [InlineData("notinstalled")]
    [InlineData("failed")]
    public void when_there_is_no_update_to_install_then_nothing_is_done_whatever_was_deferred(string kind)
    {
        var result = kind switch
        {
            "uptodate" => UpdateCheckResultFactory.CreateUpToDate(),
            "notinstalled" => UpdateCheckResultFactory.CreateNotInstalled(),
            _ => UpdateCheckResultFactory.CreateFailed("offline"),
        };

        Decide(result, UpdateDeferralFactory.CreateDeferredSince("1.2.3", Now.AddDays(-30), Now.AddDays(-30))).ShouldBe(UpdateActionFactory.CreateNone());
    }

    [Fact]
    public void when_a_deferral_is_created_with_a_last_question_before_the_first_deferral_then_the_first_deferral_is_used()
    {
        var deferral = UpdateDeferralFactory.CreateDeferredSince("1.2.3", Now, Now.AddDays(-1)).ShouldBeOfType<DeferredSince>();

        (deferral.FirstDeferredAt, deferral.LastPromptedAt).ShouldBe((Now, Now));
    }

    [Fact]
    public void when_a_deferral_is_created_without_a_version_then_the_version_is_empty_text()
    {
        UpdateDeferralFactory.CreateDeferredSince(null, Now, Now).ShouldBeOfType<DeferredSince>().Version.ShouldBe(string.Empty);
    }
}
