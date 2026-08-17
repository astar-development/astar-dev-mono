using Fab4Kids.Web.Fulfilment;

namespace Fab4Kids.Web.TestsUnit.Fulfilment;

public class GivenADeliveryLinkFactory
{
    [Fact]
    public void when_created_with_valid_values_then_they_are_preserved()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        var sut = DeliveryLinkFactory.Create("Times Tables Pack", "https://example.blob.core.windows.net/pdfs/file1.pdf?sig=abc", expiresAt);

        sut.ProductTitle.ShouldBe("Times Tables Pack");
        sut.Url.ShouldBe("https://example.blob.core.windows.net/pdfs/file1.pdf?sig=abc");
        sut.ExpiresAt.ShouldBe(expiresAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_with_a_blank_product_title_then_it_is_normalized(string? productTitle)
    {
        var sut = DeliveryLinkFactory.Create(productTitle, "https://example.blob.core.windows.net/pdfs/file1.pdf", DateTimeOffset.UtcNow);

        sut.ProductTitle.ShouldBe("Resource");
    }
}
