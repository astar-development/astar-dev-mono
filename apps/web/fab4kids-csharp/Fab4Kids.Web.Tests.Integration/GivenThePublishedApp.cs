using Microsoft.Playwright;

namespace Fab4Kids.Web.Tests.Integration;

public sealed class GivenThePublishedApp(SmokeTestFixture fixture) : IClassFixture<SmokeTestFixture>
{
    public static bool AppConfigured => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FAB4KIDS_SMOKE_URL"));

    [Fact(SkipUnless = nameof(AppConfigured), Skip = "Requires FAB4KIDS_SMOKE_URL pointing at a running published app")]
    public async Task when_the_home_page_loads_then_there_are_no_javascript_errors()
    {
        var (page, consoleErrors, pageErrors) = await CreateInstrumentedPageAsync();

        await GotoAndAwaitInteractiveAsync(page, fixture.BaseUrl!, ".hero__headline");

        consoleErrors.ShouldBeEmpty();
        pageErrors.ShouldBeEmpty();

        await page.CloseAsync();
    }

    [Fact(SkipUnless = nameof(AppConfigured), Skip = "Requires FAB4KIDS_SMOKE_URL pointing at a running published app")]
    public async Task when_a_subject_page_loads_then_there_are_no_javascript_errors()
    {
        var (page, consoleErrors, pageErrors) = await CreateInstrumentedPageAsync();

        await GotoAndAwaitInteractiveAsync(page, $"{fixture.BaseUrl}/maths", ".pdf-card");

        consoleErrors.ShouldBeEmpty();
        pageErrors.ShouldBeEmpty();

        await page.CloseAsync();
    }

    [Fact(SkipUnless = nameof(AppConfigured), Skip = "Requires FAB4KIDS_SMOKE_URL pointing at a running published app")]
    public async Task when_a_pdf_is_added_to_the_basket_and_then_removed_then_the_basket_becomes_empty_again()
    {
        var (page, consoleErrors, pageErrors) = await CreateInstrumentedPageAsync();

        await GotoAndAwaitInteractiveAsync(page, $"{fixture.BaseUrl}/maths", ".pdf-card");

        var addToBasketButton = page.Locator(".pdf-card button", new PageLocatorOptions { HasText = "Add +" }).First;
        await addToBasketButton.ClickAsync();
        await page.Locator(".pdf-card button", new PageLocatorOptions { HasText = "Added" }).First.WaitForAsync();

        var cartBadge = page.Locator(".cart-widget__badge");
        await cartBadge.WaitForAsync();
        (await cartBadge.InnerTextAsync()).ShouldBe("1");

        await page.Locator(".cart-widget__toggle").ClickAsync();
        await page.Locator(".cart-widget__remove").WaitForAsync();
        await page.Locator(".cart-widget__remove").ClickAsync();
        await page.Locator(".cart-widget__empty").WaitForAsync();

        (await cartBadge.CountAsync()).ShouldBe(0);
        consoleErrors.ShouldBeEmpty();
        pageErrors.ShouldBeEmpty();

        await page.CloseAsync();
    }

    [Fact(SkipUnless = nameof(AppConfigured), Skip = "Requires FAB4KIDS_SMOKE_URL pointing at a running published app")]
    public async Task when_checkout_is_attempted_without_stripe_configured_then_a_friendly_error_is_shown()
    {
        var (page, consoleErrors, pageErrors) = await CreateInstrumentedPageAsync();

        await GotoAndAwaitInteractiveAsync(page, $"{fixture.BaseUrl}/maths", ".pdf-card");
        await page.Locator(".pdf-card button", new PageLocatorOptions { HasText = "Add +" }).First.ClickAsync();
        await page.Locator(".cart-widget__badge").WaitForAsync();

        await page.Locator(".cart-widget__toggle").ClickAsync();
        await page.Locator(".cart-widget__checkout").ClickAsync();
        var checkoutErrorMessage = page.Locator(".cart-widget__error");
        await checkoutErrorMessage.WaitForAsync();

        (await checkoutErrorMessage.InnerTextAsync()).ShouldBe("Checkout is currently unavailable.");
        consoleErrors.ShouldBeEmpty();
        pageErrors.ShouldBeEmpty();

        await page.CloseAsync();
    }

    [Fact(SkipUnless = nameof(AppConfigured), Skip = "Requires FAB4KIDS_SMOKE_URL pointing at a running published app")]
    public async Task when_the_newsletter_form_is_submitted_with_an_invalid_email_then_a_validation_error_is_shown()
    {
        var (page, consoleErrors, pageErrors) = await CreateInstrumentedPageAsync();

        await GotoAndAwaitInteractiveAsync(page, $"{fixture.BaseUrl}/#newsletter", "#newsletter-email");
        await page.Locator("#newsletter-email").FillAsync("test@test");
        await page.Locator(".checkbox-input").CheckAsync();
        await page.Locator(".newsletter-form button[type=submit]").ClickAsync();

        var emailError = page.Locator("#newsletter-email-error");
        await emailError.WaitForAsync();

        (await emailError.InnerTextAsync()).ShouldBe("Please enter a valid email address.");
        consoleErrors.ShouldBeEmpty();
        pageErrors.ShouldBeEmpty();

        await page.CloseAsync();
    }

    private async Task<(IPage Page, List<string> ConsoleErrors, List<string> PageErrors)> CreateInstrumentedPageAsync()
    {
        var page = await fixture.Browser.NewPageAsync();
        page.SetDefaultTimeout(60000);
        var consoleErrors = new List<string>();
        var pageErrors = new List<string>();

        page.Console += (_, message) =>
        {
            if (message.Type == "error")
                consoleErrors.Add(message.Text);
        };
        page.PageError += (_, error) => pageErrors.Add(error);

        return (page, consoleErrors, pageErrors);
    }

    private static async Task GotoAndAwaitInteractiveAsync(IPage page, string url, string readySelector)
    {
        await page.GotoAsync(url);
        await page.Locator(readySelector).First.WaitForAsync();
        await AwaitInteractiveServerCircuitToConnectAsync(page);
    }

    private static Task AwaitInteractiveServerCircuitToConnectAsync(IPage page) => page.WaitForTimeoutAsync(750);
}
