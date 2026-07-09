using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Serilog;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Models;

public sealed class GivenTheLogFailureExtension
{
    [Fact]
    public void when_the_result_is_a_success_then_the_logger_does_not_receive_an_error_call()
    {
        var logger = Substitute.For<ILogger>();
        var result = Result.Success<string, ScrapeError>("some value");

        result.LogFailure(logger);

        logger.DidNotReceive().Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_the_result_is_a_success_then_the_same_result_is_returned()
    {
        var logger = Substitute.For<ILogger>();
        var result = Result.Success<string, ScrapeError>("some value");

        var returned = result.LogFailure(logger);

        returned.ShouldBeSameAs(result);
    }

    [Fact]
    public void when_the_result_is_a_failure_then_the_logger_receives_exactly_one_error_call()
    {
        var logger = Substitute.For<ILogger>();
        var result = Result.Failure<string, ScrapeError>(ScrapeErrorFactory.CreateConfigurationSaveFailed("boom"));

        result.LogFailure(logger);

        logger.Received(1).Error(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void when_the_result_is_a_failure_then_the_same_result_is_returned()
    {
        var logger = Substitute.For<ILogger>();
        var result = Result.Failure<string, ScrapeError>(ScrapeErrorFactory.CreateConfigurationSaveFailed("boom"));

        var returned = result.LogFailure(logger);

        returned.ShouldBeSameAs(result);
    }
}
