using AStar.Dev.Logging.Extensions;
using Azure.Communication.Email;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Blazored.LocalStorage;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Checkout;
using Fab4Kids.Web.Components;
using Fab4Kids.Web.Consent;
using Fab4Kids.Web.Fulfilment;
using Fab4Kids.Web.Newsletter;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;
using Stripe;
using Stripe.Checkout;

var builder = WebApplication.CreateBuilder(args);
builder.AddSerilogLogging();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<CookieConsentState>();
builder.Services.AddScoped<CartState>();
builder.Services.AddSingleton<ICatalogueService, CatalogueService>();

builder.Services.Configure<NewsletterOptions>(builder.Configuration.GetSection("Newsletter"));
var newsletterOptions = builder.Configuration.GetSection("Newsletter").Get<NewsletterOptions>();
if (!string.IsNullOrWhiteSpace(newsletterOptions?.TableStorageConnectionString) && !string.IsNullOrWhiteSpace(newsletterOptions.TableName))
    builder.Services.AddSingleton(new TableClient(newsletterOptions.TableStorageConnectionString, newsletterOptions.TableName));
if (!string.IsNullOrWhiteSpace(newsletterOptions?.EmailConnectionString))
    builder.Services.AddSingleton(new EmailClient(newsletterOptions.EmailConnectionString));
builder.Services.AddSingleton<INewsletterSubscriberStore, AzureTableNewsletterSubscriberStore>();
builder.Services.AddSingleton<INewsletterEmailSender, AzureNewsletterEmailSender>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<INewsletterSubscriptionService, NewsletterSubscriptionService>();

builder.Services.Configure<CheckoutOptions>(builder.Configuration.GetSection("Checkout"));
var checkoutOptions = builder.Configuration.GetSection("Checkout").Get<CheckoutOptions>();
if (!string.IsNullOrWhiteSpace(checkoutOptions?.SecretKey))
    builder.Services.AddSingleton(new SessionService(new StripeClient(checkoutOptions.SecretKey)));
builder.Services.AddScoped<ICheckoutSessionService, StripeCheckoutSessionService>();

builder.Services.Configure<FulfilmentOptions>(builder.Configuration.GetSection("Fulfilment"));
var fulfilmentOptions = builder.Configuration.GetSection("Fulfilment").Get<FulfilmentOptions>();
if (!string.IsNullOrWhiteSpace(fulfilmentOptions?.StorageConnectionString) && !string.IsNullOrWhiteSpace(fulfilmentOptions.BlobContainerName))
    builder.Services.AddSingleton(new BlobContainerClient(fulfilmentOptions.StorageConnectionString, fulfilmentOptions.BlobContainerName));
builder.Services.AddSingleton<IPdfDeliveryLinkGenerator, BlobSasDeliveryLinkGenerator>();
builder.Services.AddSingleton<IIdempotencyStore>(sp =>
{
    var idempotencyLogger = sp.GetRequiredService<ILogger<AzureTableIdempotencyStore>>();
    var idempotencyTableClient = !string.IsNullOrWhiteSpace(fulfilmentOptions?.StorageConnectionString) && !string.IsNullOrWhiteSpace(fulfilmentOptions.IdempotencyTableName)
        ? new TableClient(fulfilmentOptions.StorageConnectionString, fulfilmentOptions.IdempotencyTableName)
        : null;

    return new AzureTableIdempotencyStore(idempotencyLogger, idempotencyTableClient);
});
builder.Services.AddSingleton<IDeliveryEmailSender>(sp =>
{
    var deliveryEmailLogger = sp.GetRequiredService<ILogger<AzureDeliveryEmailSender>>();
    var deliveryEmailClient = !string.IsNullOrWhiteSpace(fulfilmentOptions?.EmailConnectionString) ? new EmailClient(fulfilmentOptions.EmailConnectionString) : null;

    return new AzureDeliveryEmailSender(sp.GetRequiredService<IOptions<FulfilmentOptions>>(), deliveryEmailLogger, deliveryEmailClient);
});
builder.Services.AddScoped<IFulfilmentService, FulfilmentService>();
builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(60));

var app = builder.Build();

app.Services.GetRequiredService<ICatalogueService>();

app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseHsts();
app.UseAntiforgery();

app.UseSerilogRequestLogging();

app.MapStaticAssets();
app.MapCheckoutEndpoints();
app.MapFulfilmentEndpoints();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

try
{
    await app.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
