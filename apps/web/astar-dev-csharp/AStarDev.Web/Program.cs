using AStarDev.Web.CaseStudies;
using AStarDev.Web.Components;
using AStarDev.Web.Consent;
using AStarDev.Web.Contact;
using AStarDev.Web.Packages;
using AStarDev.Web.Theming;
using Azure.Communication.Email;
using Blazored.LocalStorage;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<CookieConsentState>();
builder.Services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
builder.Services.AddHttpClient<INugetApiClient, NugetApiClient>(client => client.BaseAddress = new Uri("https://azuresearch-usnc.nuget.org/"));
builder.Services.AddScoped<INugetPackageService, NugetPackageService>();

builder.Services.Configure<ContactFormOptions>(builder.Configuration.GetSection("ContactForm"));
var contactFormOptions = builder.Configuration.GetSection("ContactForm").Get<ContactFormOptions>();
if (!string.IsNullOrWhiteSpace(contactFormOptions?.ConnectionString))
    builder.Services.AddSingleton(new EmailClient(contactFormOptions.ConnectionString));
builder.Services.AddSingleton<IContactRateLimiter, ContactRateLimiter>();
builder.Services.AddScoped<IContactEmailSender, AzureContactEmailSender>();
builder.Services.AddScoped<IContactSubmissionService, ContactSubmissionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
