using AStar.Dev.FunctionalParadigm;
using Fab4Kids.Web.Newsletter;
using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Common;

public sealed partial class NewsletterForm : ComponentBase
{
    private string email = string.Empty;
    private bool consent;

    private string emailError = string.Empty;
    private string consentError = string.Empty;

    private NewsletterFormSubmitStatus submitStatus = NewsletterFormSubmitStatus.Idle;
    private string statusMessage = string.Empty;

    private async Task HandleSubmitAsync()
    {
        emailError = string.Empty;
        consentError = string.Empty;
        submitStatus = NewsletterFormSubmitStatus.Submitting;
        statusMessage = string.Empty;

        var outcome = await SubscriptionService.SubscribeAsync(email, consent, CancellationToken.None);

        switch (outcome)
        {
            case NewsletterSubscriptionSucceeded or NewsletterSubscriptionAlreadySubscribed:
                submitStatus = NewsletterFormSubmitStatus.Success;
                statusMessage = "\U0001F389 You're on the list!";
                email = string.Empty;
                consent = false;
                break;

            case NewsletterSubscriptionNoConsent:
                submitStatus = NewsletterFormSubmitStatus.Idle;
                consentError = "Please check the consent box to subscribe.";
                break;

            case NewsletterSubscriptionValidationFailed validationFailed:
                submitStatus = NewsletterFormSubmitStatus.Idle;
                ApplyValidationErrors(validationFailed.Errors);
                break;

            case NewsletterSubscriptionSubscribeFailed subscribeFailed:
                submitStatus = NewsletterFormSubmitStatus.Error;
                statusMessage = subscribeFailed.Message;
                break;
        }
    }

    private void ApplyValidationErrors(IReadOnlyList<ValidationError> errors)
    {
        foreach (var error in errors)
        {
            if (error.Property == "email")
                emailError = error.Message;
        }
    }
}
