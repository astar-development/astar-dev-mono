using Fab4Kids.Web.Cart;

namespace Fab4Kids.Web.TestsUnit.Cart;

public class GivenACartItemFactory
{
    [Fact]
    public void when_created_with_valid_values_then_they_are_preserved()
    {
        var sut = CartItemFactory.Create(1, "Times Tables Pack", 2.50m, 2);

        sut.ProductId.ShouldBe(1);
        sut.Name.ShouldBe("Times Tables Pack");
        sut.Price.ShouldBe(2.50m);
        sut.Quantity.ShouldBe(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_with_a_blank_name_then_it_is_normalized(string? name)
    {
        var sut = CartItemFactory.Create(1, name, 1m, 1);

        sut.Name.ShouldBe("Untitled resource");
    }

    [Fact]
    public void when_created_with_a_negative_price_then_it_is_clamped_to_zero()
    {
        var sut = CartItemFactory.Create(1, "File", -5m, 1);

        sut.Price.ShouldBe(0m);
    }

    [Fact]
    public void when_created_with_a_quantity_below_one_then_it_is_clamped_to_one()
    {
        var sut = CartItemFactory.Create(1, "File", 1m, 0);

        sut.Quantity.ShouldBe(1);
    }

    [Fact]
    public void when_created_with_a_blob_path_then_it_is_preserved()
    {
        var sut = CartItemFactory.Create(1, "File", 1m, 1, "pdfs/file1.pdf");

        sut.BlobPath.ShouldBe("pdfs/file1.pdf");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_without_a_blob_path_then_it_defaults_to_empty(string? blobPath)
    {
        var sut = CartItemFactory.Create(1, "File", 1m, 1, blobPath);

        sut.BlobPath.ShouldBe(string.Empty);
    }
}
