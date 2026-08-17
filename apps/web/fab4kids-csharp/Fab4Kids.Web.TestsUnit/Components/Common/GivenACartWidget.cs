using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Checkout;
using Fab4Kids.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.TestsUnit.Components.Common;

public class GivenACartWidget : Bunit.BunitContext
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();
    private readonly ICheckoutSessionService checkoutSessionService = Substitute.For<ICheckoutSessionService>();
    private readonly CartState cartState;

    public GivenACartWidget()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        cartState = new CartState(localStorage);
        Services.AddSingleton(cartState);
        Services.AddSingleton(checkoutSessionService);
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

    [Fact]
    public async Task when_checkout_succeeds_then_the_browser_is_redirected_to_the_stripe_session_url()
    {
        checkoutSessionService.CreateSessionAsync(Arg.Any<IReadOnlyList<CartItem>>(), Arg.Any<CancellationToken>())
            .Returns(new CheckoutSessionCreated("https://checkout.stripe.com/pay/cs_test_123"));
        var cut = Render<CartWidget>();
        await cut.InvokeAsync(() => cartState.AddItemAsync(1, "Times Tables Pack", 2.50m));
        cut.Find("button.cart-widget__toggle").Click();
        var navigationManager = Services.GetRequiredService<NavigationManager>();

        cut.Find("button.cart-widget__checkout").Click();

        navigationManager.Uri.ShouldBe("https://checkout.stripe.com/pay/cs_test_123");
    }

    [Fact]
    public async Task when_checkout_fails_then_an_error_message_is_shown()
    {
        checkoutSessionService.CreateSessionAsync(Arg.Any<IReadOnlyList<CartItem>>(), Arg.Any<CancellationToken>())
            .Returns(new CheckoutSessionFailed("Checkout is currently unavailable."));
        var cut = Render<CartWidget>();
        await cut.InvokeAsync(() => cartState.AddItemAsync(1, "Times Tables Pack", 2.50m));
        cut.Find("button.cart-widget__toggle").Click();

        cut.Find("button.cart-widget__checkout").Click();

        cut.Find("p.cart-widget__error").TextContent.ShouldBe("Checkout is currently unavailable.");
    }
}
