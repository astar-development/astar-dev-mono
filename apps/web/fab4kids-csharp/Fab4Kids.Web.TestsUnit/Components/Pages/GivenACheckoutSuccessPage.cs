using AStar.Dev.FunctionalParadigm;
using Bunit;
using Fab4Kids.Web.Checkout;
using Fab4Kids.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.TestsUnit.Components.Pages;

public class GivenACheckoutSuccessPage : Bunit.BunitContext
{
    private readonly ICheckoutSessionService checkoutSessionService = Substitute.For<ICheckoutSessionService>();

    public GivenACheckoutSuccessPage() => Services.AddSingleton(checkoutSessionService);

    private void Navigate(string uri) => Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

    [Fact]
    public void when_no_session_id_is_present_then_the_visitor_is_redirected_home()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();

        Render<CheckoutSuccess>();

        navigationManager.Uri.ShouldBe(navigationManager.BaseUri);
    }

    [Fact]
    public void when_the_session_has_a_customer_email_then_it_is_shown()
    {
        checkoutSessionService.GetCustomerEmailAsync("cs_test_123", Arg.Any<CancellationToken>()).Returns(Option.Some("ada@example.com"));
        Navigate("/checkout/success?session_id=cs_test_123");

        var cut = Render<CheckoutSuccess>();

        cut.Find("p.success-lead").TextContent.ShouldContain("ada@example.com");
    }

    [Fact]
    public void when_the_session_has_no_customer_email_then_a_generic_message_is_shown()
    {
        checkoutSessionService.GetCustomerEmailAsync("cs_test_123", Arg.Any<CancellationToken>()).Returns(Option.None<string>());
        Navigate("/checkout/success?session_id=cs_test_123");

        var cut = Render<CheckoutSuccess>();

        cut.Find("p.success-lead").TextContent.ShouldBe("Thank you for your order. Download links have been sent to your email address.");
    }
}
