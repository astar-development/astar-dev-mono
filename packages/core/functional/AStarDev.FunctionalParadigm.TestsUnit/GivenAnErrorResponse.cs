namespace AStar.Dev.FunctionalParadigm.TestsUnit;

public sealed class GivenAnErrorResponse
{
    [Fact]
    public void when_constructed_with_a_message_then_the_message_property_returns_it() =>
        new ErrorResponse("something went wrong").Message.ShouldBe("something went wrong");

    [Fact]
    public void when_two_error_responses_have_the_same_message_then_they_are_equal() =>
        new ErrorResponse("failure").ShouldBe(new ErrorResponse("failure"));

    [Fact]
    public void when_two_error_responses_have_different_messages_then_they_are_not_equal() =>
        new ErrorResponse("failure").ShouldNotBe(new ErrorResponse("other failure"));
}
