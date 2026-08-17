using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.Components.Common;
using AStarDev.Web.Contact;
using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AStarDev.Web.TestsUnit.Components.Common;

public class GivenAContactForm : Bunit.BunitContext
{
    private readonly IContactSubmissionService submissionService = Substitute.For<IContactSubmissionService>();

    public GivenAContactForm()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());

        Services.AddSingleton(submissionService);
        Services.AddSingleton(httpContextAccessor);
    }

    [Fact]
    public void when_rendered_then_the_required_fields_and_submit_button_are_shown()
    {
        var cut = Render<ContactForm>();

        cut.Find("#name").ShouldNotBeNull();
        cut.Find("#email").ShouldNotBeNull();
        cut.Find("#message").ShouldNotBeNull();
        cut.Find("button.btn-submit").TextContent.ShouldContain("Send message");
    }

    [Fact]
    public void when_submission_succeeds_then_a_success_message_is_shown()
    {
        submissionService.SubmitAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ContactSubmissionOutcomeFactory.Succeeded());
        var cut = Render<ContactForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find(".status-message--success").TextContent.ShouldContain("Thank you for your message."));
    }

    [Fact]
    public void when_the_rate_limit_is_exceeded_then_the_throttle_message_is_shown()
    {
        submissionService.SubmitAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ContactSubmissionOutcomeFactory.RateLimited());
        var cut = Render<ContactForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find(".status-message--error").TextContent.ShouldContain("Too many requests"));
    }

    [Fact]
    public void when_sending_fails_then_the_returned_message_is_shown()
    {
        submissionService.SubmitAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ContactSubmissionOutcomeFactory.SendFailed("Something went wrong. Please email owner@astardevelopment.co.uk directly."));
        var cut = Render<ContactForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find(".status-message--error").TextContent.ShouldContain("email owner@astardevelopment.co.uk directly"));
    }

    [Fact]
    public void when_validation_fails_then_the_field_errors_are_shown_and_no_status_banner_appears()
    {
        submissionService.SubmitAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ContactSubmissionOutcomeFactory.ValidationFailed([ValidationErrorFactory.Create("name", "Name is required.")]));
        var cut = Render<ContactForm>();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("#name-error").TextContent.ShouldBe("Name is required."));
        cut.FindAll(".status-message").ShouldBeEmpty();
    }
}
