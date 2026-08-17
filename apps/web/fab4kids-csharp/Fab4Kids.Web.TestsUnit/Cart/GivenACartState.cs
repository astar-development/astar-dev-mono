using Blazored.LocalStorage;
using Fab4Kids.Web.Cart;
using NSubstitute;

namespace Fab4Kids.Web.TestsUnit.Cart;

public class GivenACartState
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();

    [Fact]
    public void when_constructed_then_items_is_empty()
    {
        var sut = new CartState(localStorage);

        sut.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_initialized_and_nothing_is_stored_then_items_stays_empty()
    {
        localStorage.GetItemAsync<IReadOnlyList<CartItem>>("fab4kids-cart", Arg.Any<CancellationToken>()).Returns((IReadOnlyList<CartItem>?)null);
        var sut = new CartState(localStorage);

        await sut.InitializeAsync();

        sut.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_initialized_and_items_are_stored_then_items_are_loaded()
    {
        CartItem[] stored = [CartItemFactory.Create(1, "Times Tables Pack", 2.50m, 2)];
        localStorage.GetItemAsync<IReadOnlyList<CartItem>>("fab4kids-cart", Arg.Any<CancellationToken>()).Returns(stored);
        var sut = new CartState(localStorage);

        await sut.InitializeAsync();

        sut.Items.ShouldBe(stored);
    }

    [Fact]
    public async Task when_a_new_item_is_added_then_it_appears_with_quantity_one()
    {
        var sut = new CartState(localStorage);

        await sut.AddItemAsync(1, "Times Tables Pack", 2.50m);

        sut.Items.ShouldHaveSingleItem();
        sut.Items[0].ProductId.ShouldBe(1);
        sut.Items[0].Quantity.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_new_item_is_added_with_a_blob_path_then_it_is_preserved()
    {
        var sut = new CartState(localStorage);

        await sut.AddItemAsync(1, "Times Tables Pack", 2.50m, "pdfs/file1.pdf");

        sut.Items[0].BlobPath.ShouldBe("pdfs/file1.pdf");
    }

    [Fact]
    public async Task when_an_existing_item_is_added_again_then_its_quantity_increments()
    {
        var sut = new CartState(localStorage);
        await sut.AddItemAsync(1, "Times Tables Pack", 2.50m);

        await sut.AddItemAsync(1, "Times Tables Pack", 2.50m);

        sut.Items.ShouldHaveSingleItem();
        sut.Items[0].Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task when_an_item_is_added_then_it_is_persisted_to_local_storage()
    {
        var sut = new CartState(localStorage);

        await sut.AddItemAsync(1, "Times Tables Pack", 2.50m);

        await localStorage.Received(1).SetItemAsync("fab4kids-cart", Arg.Any<IReadOnlyList<CartItem>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_an_item_is_added_then_on_change_is_raised()
    {
        var sut = new CartState(localStorage);
        bool raised = false;
        sut.OnChange += () => raised = true;

        await sut.AddItemAsync(1, "Times Tables Pack", 2.50m);

        raised.ShouldBeTrue();
    }

    [Fact]
    public async Task when_an_item_is_removed_then_it_is_no_longer_present()
    {
        var sut = new CartState(localStorage);
        await sut.AddItemAsync(1, "Times Tables Pack", 2.50m);
        await sut.AddItemAsync(2, "English Pack", 3.00m);

        await sut.RemoveItemAsync(1);

        sut.Items.ShouldHaveSingleItem();
        sut.Items[0].ProductId.ShouldBe(2);
    }

    [Fact]
    public async Task when_items_are_present_then_total_items_and_total_price_are_computed()
    {
        var sut = new CartState(localStorage);
        await sut.AddItemAsync(1, "Times Tables Pack", 2.50m);
        await sut.AddItemAsync(1, "Times Tables Pack", 2.50m);
        await sut.AddItemAsync(2, "English Pack", 3.00m);

        sut.TotalItems.ShouldBe(3);
        sut.TotalPrice.ShouldBe(8.00m);
    }
}
