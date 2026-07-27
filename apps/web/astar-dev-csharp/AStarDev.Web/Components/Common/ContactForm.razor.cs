using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.Contact;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AStarDev.Web.Components.Common;

public partial class ContactForm : ComponentBase
{
    private const string FocusInterop = "astarFocus.focus";

    private string name = string.Empty;
    private string email = string.Empty;
    private string message = string.Empty;
    private bool sendCopy;
    private string website = string.Empty;

    private string nameError = string.Empty;
    private string emailError = string.Empty;
    private string messageError = string.Empty;

    private ContactFormSubmitStatus submitStatus = ContactFormSubmitStatus.Idle;
    private string statusMessage = string.Empty;
    private string ipAddress = "unknown";

    private ElementReference nameElement;
    private ElementReference emailElement;
    private ElementReference messageElement;
    private ElementReference statusElement;

    protected override void OnInitialized()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        ipAddress = httpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
            ?? httpContext?.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }

    private async Task HandleSubmitAsync()
    {
        nameError = string.Empty;
        emailError = string.Empty;
        messageError = string.Empty;
        submitStatus = ContactFormSubmitStatus.Submitting;
        statusMessage = string.Empty;

        var outcome = await SubmissionService.SubmitAsync(name, email, message, sendCopy, website, ipAddress, CancellationToken.None);

        switch (outcome)
        {
            case ContactSubmissionSucceeded:
                submitStatus = ContactFormSubmitStatus.Success;
                statusMessage = "Thank you for your message. We will be in touch soon.";
                name = string.Empty;
                email = string.Empty;
                message = string.Empty;
                sendCopy = false;
                website = string.Empty;
                break;

            case ContactSubmissionRateLimited:
                submitStatus = ContactFormSubmitStatus.Error;
                statusMessage = "Too many requests. Please try again in 15 minutes.";
                break;

            case ContactSubmissionValidationFailed validationFailed:
                submitStatus = ContactFormSubmitStatus.Idle;
                ApplyValidationErrors(validationFailed.Errors);
                await FocusFirstInvalidFieldAsync();
                break;

            case ContactSubmissionSendFailed sendFailed:
                submitStatus = ContactFormSubmitStatus.Error;
                statusMessage = sendFailed.Message;
                break;
        }

        StateHasChanged();

        if (submitStatus is ContactFormSubmitStatus.Success or ContactFormSubmitStatus.Error)
        {
            await Task.Delay(50);
            await JsRuntime.InvokeVoidAsync(FocusInterop, statusElement);
        }
    }

    private void ApplyValidationErrors(IReadOnlyList<ValidationError> errors)
    {
        foreach (var error in errors)
        {
            switch (error.Property)
            {
                case "name":
                    nameError = error.Message;
                    break;
                case "email":
                    emailError = error.Message;
                    break;
                case "message":
                    messageError = error.Message;
                    break;
            }
        }
    }

    private async Task FocusFirstInvalidFieldAsync()
    {
        if (nameError.Length > 0)
            await JsRuntime.InvokeVoidAsync(FocusInterop, nameElement);
        else if (emailError.Length > 0)
            await JsRuntime.InvokeVoidAsync(FocusInterop, emailElement);
        else if (messageError.Length > 0)
            await JsRuntime.InvokeVoidAsync(FocusInterop, messageElement);
    }
}
