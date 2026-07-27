using AStar.Dev.Logging.Extensions;
using Blazored.LocalStorage;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Components;
using Fab4Kids.Web.Consent;
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
builder.Services.AddSingleton<ICatalogueService, CatalogueService>();

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
