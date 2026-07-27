using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Components.Common;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Common;

public class GivenACartWidget : Bunit.BunitContext
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();
    private readonly CartState cartState;

    public GivenACartWidget()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        cartState = new CartState(localStorage);
        Services.AddSingleton(cartState);
    }

    [Fact]
    public void when_the_basket_is_empty_then_no_badge_is_shown()
    {
        var cut = Render<CartWidget>();

        cut.FindAll("span.cart-widget__badge").ShouldBeEmpty();
    }

    [Fact]
    public async Task when_items_are_in_the_basket_then_the_badge_shows_the_total()
    {
        var cut = Render<CartWidget>();

        await cut.InvokeAsync(() => cartState.AddItemAsync(1, "Times Tables Pack", 2.50m));

        cut.Find("span.cart-widget__badge").TextContent.ShouldBe("1");
    }

    [Fact]
    public void when_the_toggle_is_clicked_then_the_drawer_opens()
    {
        var cut = Render<CartWidget>();

        cut.Find("button.cart-widget__toggle").Click();

        cut.FindAll("div.cart-widget__drawer").Count.ShouldBe(1);
    }

    [Fact]
    public void when_the_drawer_is_open_and_the_basket_is_empty_then_the_empty_message_is_shown()
    {
        var cut = Render<CartWidget>();

        cut.Find("button.cart-widget__toggle").Click();

        cut.Find("p.cart-widget__empty").TextContent.ShouldBe("Your basket is empty.");
    }

    [Fact]
    public async Task when_the_drawer_is_open_and_items_are_present_then_they_are_listed_with_the_total()
    {
        var cut = Render<CartWidget>();
        await cut.InvokeAsync(() => cartState.AddItemAsync(1, "Times Tables Pack", 2.50m));

        cut.Find("button.cart-widget__toggle").Click();

        cut.Find("span.cart-widget__item-name").TextContent.ShouldBe("Times Tables Pack");
        cut.Find("p.cart-widget__total").TextContent.ShouldContain("£2.50");
    }

    [Fact]
    public async Task when_remove_is_clicked_then_the_item_is_removed_from_the_basket()
    {
        var cut = Render<CartWidget>();
        await cut.InvokeAsync(() => cartState.AddItemAsync(1, "Times Tables Pack", 2.50m));
        cut.Find("button.cart-widget__toggle").Click();

        cut.Find("button.cart-widget__remove").Click();

        cartState.Items.ShouldBeEmpty();
    }
}
