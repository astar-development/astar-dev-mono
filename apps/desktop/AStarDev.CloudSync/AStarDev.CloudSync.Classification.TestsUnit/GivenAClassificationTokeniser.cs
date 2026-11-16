namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class GivenAClassificationTokeniser
{
    [Theory]
    [InlineData("red_car-photo.final", new[] { "red", "car", "photo", "final" })]
    [InlineData("RedCarPhoto", new[] { "red", "car", "photo" })]
    [InlineData("HTMLFile", new[] { "html", "fil" })]
    [InlineData("IMG_2023_beach", new[] { "img", "beach" })]
    [InlineData("the car and the house", new[] { "car", "hous" })]
    [InlineData("Cars CAR car", new[] { "car" })]
    [InlineData("babies boxes dresses bus glass", new[] { "baby", "box", "dress", "bus", "glass" })]
    [InlineData("a b 123", new string[0])]
    [InlineData("mp3 collection", new[] { "mp3", "collection" })]
    [InlineData("4k photo", new[] { "4k", "photo" })]
    [InlineData("Photo2023 trip", new[] { "photo", "trip" })]
    [InlineData("2024beach", new[] { "beach" })]
    [InlineData("mp3Collection", new[] { "mp3", "collection" })]
    [InlineData("", new string[0])]
    public void when_text_is_tokenised_then_the_expected_tokens_are_returned(string text, string[] expected) =>
        ClassificationTokeniser.Tokenise(text).ShouldBe(expected);

    [Fact]
    public void when_text_is_null_then_no_tokens_are_returned() =>
        ClassificationTokeniser.Tokenise(null).ShouldBeEmpty();
}
