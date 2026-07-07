using AStar.Dev.Wallpaper.Scrapper.Workflows;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Workflows;

public sealed class GivenAScrapeSiteWorkflowDecision
{
    [Fact]
    public async Task when_the_setup_step_fails_then_the_workflow_is_never_invoked()
    {
        int workflowInvocationCount = 0;

        Task<global::AStar.Dev.FunctionalParadigm.Result<global::AStar.Dev.FunctionalParadigm.Unit, string>> Workflow(CancellationToken ct)
        {
            workflowInvocationCount++;

            return Task.FromResult<global::AStar.Dev.FunctionalParadigm.Result<global::AStar.Dev.FunctionalParadigm.Unit, string>>(global::AStar.Dev.FunctionalParadigm.Unit.Value);
        }

        await ScrapeSiteWorkflowDecision.DecideAsync(new InvalidOperationException("setup failed"), Workflow);

        workflowInvocationCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_setup_step_succeeds_then_the_workflow_is_invoked()
    {
        using var cts = new CancellationTokenSource();
        int workflowInvocationCount = 0;

        Task<global::AStar.Dev.FunctionalParadigm.Result<global::AStar.Dev.FunctionalParadigm.Unit, string>> Workflow(CancellationToken ct)
        {
            workflowInvocationCount++;

            return Task.FromResult<global::AStar.Dev.FunctionalParadigm.Result<global::AStar.Dev.FunctionalParadigm.Unit, string>>(global::AStar.Dev.FunctionalParadigm.Unit.Value);
        }

        await ScrapeSiteWorkflowDecision.DecideAsync(cts.Token, Workflow);

        workflowInvocationCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_setup_step_fails_then_the_result_is_a_failure_carrying_the_setup_error_message()
    {
        static Task<global::AStar.Dev.FunctionalParadigm.Result<string, string>> Workflow(CancellationToken ct)
            => Task.FromResult(global::AStar.Dev.FunctionalParadigm.Result.Success<string, string>("exported"));

        var actual = await ScrapeSiteWorkflowDecision.DecideAsync(new InvalidOperationException("setup failed"), Workflow);

        actual.ShouldBe(global::AStar.Dev.FunctionalParadigm.Result.Failure<string, string>("setup failed"));
    }

    [Fact]
    public async Task when_the_setup_step_fails_then_a_non_unit_workflow_is_never_invoked()
    {
        int workflowInvocationCount = 0;

        Task<global::AStar.Dev.FunctionalParadigm.Result<string, string>> Workflow(CancellationToken ct)
        {
            workflowInvocationCount++;

            return Task.FromResult(global::AStar.Dev.FunctionalParadigm.Result.Success<string, string>("exported"));
        }

        await ScrapeSiteWorkflowDecision.DecideAsync(new InvalidOperationException("setup failed"), Workflow);

        workflowInvocationCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_setup_step_succeeds_then_the_token_is_forwarded_to_the_workflow()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken forwardedToken = default;

        Task<global::AStar.Dev.FunctionalParadigm.Result<string, string>> Workflow(CancellationToken ct)
        {
            forwardedToken = ct;

            return Task.FromResult(global::AStar.Dev.FunctionalParadigm.Result.Success<string, string>("exported"));
        }

        await ScrapeSiteWorkflowDecision.DecideAsync(cts.Token, Workflow);

        forwardedToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task when_the_setup_step_succeeds_then_the_workflow_result_is_returned()
    {
        using var cts = new CancellationTokenSource();

        static Task<global::AStar.Dev.FunctionalParadigm.Result<string, string>> Workflow(CancellationToken ct)
            => Task.FromResult(global::AStar.Dev.FunctionalParadigm.Result.Success<string, string>("exported"));

        var actual = await ScrapeSiteWorkflowDecision.DecideAsync(cts.Token, Workflow);

        actual.ShouldBe(global::AStar.Dev.FunctionalParadigm.Result.Success<string, string>("exported"));
    }
}
