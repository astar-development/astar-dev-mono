using System.Runtime.CompilerServices;
using AStarDev.CloudSync.Logging;
using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenAnUnhandledExceptionLogger
{
    private const string Private = "sam@example.com";
    private readonly RecordingLogger<UnhandledExceptionLogger> logger = new();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Throw() => throw new InvalidOperationException(Private);

    [Fact]
    public void when_an_exception_is_logged_then_one_error_names_its_type_and_the_method_that_threw()
    {
        var thrown = Should.Throw<InvalidOperationException>(Throw);

        new UnhandledExceptionLogger(logger).Log("AppDomain", thrown);

        logger.Entries.ShouldHaveSingleItem().Level.ShouldBe(LogLevel.Error);
        logger.Messages.ShouldHaveSingleItem().ShouldBe($"AppDomain at {typeof(GivenAnUnhandledExceptionLogger).FullName}.{nameof(Throw)} encountered System.InvalidOperationException");
    }

    [Fact]
    public void when_an_exception_is_logged_then_its_message_is_not()
    {
        var thrown = Should.Throw<InvalidOperationException>(Throw);

        new UnhandledExceptionLogger(logger).Log("AppDomain", thrown);

        logger.Messages.ShouldAllBe(message => !message.Contains(Private));
    }

    [Fact]
    public void when_an_exception_was_never_thrown_then_only_the_source_and_type_are_logged()
    {
        new UnhandledExceptionLogger(logger).Log("TaskScheduler", new TimeoutException(Private));

        logger.Messages.ShouldHaveSingleItem().ShouldBe("TaskScheduler encountered System.TimeoutException");
    }
}
