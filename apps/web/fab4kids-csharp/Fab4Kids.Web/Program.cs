using AStar.Dev.Logging.Extensions;
using Azure.Communication.Email;
using Azure.Data.Tables;
using Blazored.LocalStorage;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Components;
using Fab4Kids.Web.Consent;
using Fab4Kids.Web.Newsletter;
using Fab4Kids.Web.Theming;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddSerilogLogging();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ThemeState>();
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

var app = builder.Build();

app.Services.GetRequiredService<ICatalogueService>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseSerilogRequestLogging();

app.MapStaticAssets();
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
