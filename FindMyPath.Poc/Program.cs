using System.Globalization;
using FindMyPath.Poc.Components;
using FindMyPath.Poc.Services;

// Pin the formatting culture so currency ("$"), numbers and dates render identically on every host.
// Without this, a Linux/Azure App Service instance with no locale set falls back to the invariant
// culture, whose currency symbol is the generic "¤" — so demo cost figures would show "¤0.09".
var appCulture = new CultureInfo("en-CA");
CultureInfo.DefaultThreadCurrentCulture = appCulture;
CultureInfo.DefaultThreadCurrentUICulture = appCulture;
CultureInfo.CurrentCulture = appCulture;
CultureInfo.CurrentUICulture = appCulture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<AppPaths>();
builder.Services.AddSingleton<PromptSettingsService>();
builder.Services.AddSingleton<KnowledgeBaseService>();
builder.Services.AddSingleton<HistoryService>();
builder.Services.AddSingleton<RoadmapService>();
// Per-circuit wizard/roadmap state so it survives navigating to Settings/History and back.
builder.Services.AddScoped<WizardState>();

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
