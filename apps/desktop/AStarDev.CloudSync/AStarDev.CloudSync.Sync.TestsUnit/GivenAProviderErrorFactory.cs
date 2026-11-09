using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAProviderErrorFactory
{
    [Fact]
    public void when_each_case_is_created_then_it_is_that_case()
    {
        var errors = new[]
        {
            ProviderErrorFactory.CreateAuthenticationRequired(),
            ProviderErrorFactory.CreateNotFound(),
            ProviderErrorFactory.CreateNameConflict(),
            ProviderErrorFactory.CreateTransient(),
            ProviderErrorFactory.CreateUnexpected(),
            ProviderErrorFactory.CreateUnsupported(),
            ProviderErrorFactory.CreatePreconditionFailed(),
            ProviderErrorFactory.CreateQuotaExceeded(),
            ProviderErrorFactory.CreateFileTooLarge(),
        };

        errors.Select(error => error.GetType().Name).ShouldBe(["AuthenticationRequired", "NotFound", "NameConflict", "Transient", "Unexpected", "Unsupported", "PreconditionFailed", "QuotaExceeded", "FileTooLarge"]);
    }

    [Fact]
    public void when_a_throttle_is_created_then_it_keeps_the_retry_after()
    {
        var error = ProviderErrorFactory.CreateThrottled(Option.Some(TimeSpan.FromSeconds(30)));

        error.ShouldBeOfType<ProviderError.Throttled>().RetryAfter.ShouldBe(Option.Some(TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void when_the_cursor_expired_then_the_error_keeps_the_resync_kind_and_restart_cursor()
    {
        var restart = RemoteIds.Cursor("https://graph.microsoft.com/v1.0/me/drive/root/delta?token=restart");

        var error = ProviderErrorFactory.CreateCursorExpired(ResyncKindFactory.CreateUploadDifferences(), restart);

        error.ShouldBe(ProviderErrorFactory.CreateCursorExpired(ResyncKindFactory.CreateUploadDifferences(), restart));
    }

    [Fact]
    public void when_each_resync_kind_is_created_then_it_is_that_kind()
    {
        var kinds = new[] { ResyncKindFactory.CreateApplyDifferences(), ResyncKindFactory.CreateUploadDifferences(), ResyncKindFactory.CreateUnknown() };

        kinds.Select(kind => kind.GetType().Name).ShouldBe(["ApplyDifferences", "UploadDifferences", "Unknown"]);
    }
}
