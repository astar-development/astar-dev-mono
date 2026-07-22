namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenValidationError
{
    [Fact]
    public void when_created_then_exposes_property_and_message()
    {
        var actual = ValidationErrorFactory.Create("Name", "Required");

        actual.Property.ShouldBe("Name");
        actual.Message.ShouldBe("Required");
    }
}
