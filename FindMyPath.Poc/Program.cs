using FindMyPath.Poc.Components;
using FindMyPath.Poc.Models;
using FindMyPath.Poc.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<PromptSettingsService>();
builder.Services.AddSingleton<HistoryService>();
builder.Services.AddSingleton<RoadmapService>();

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

// TEMP: verify the live Claude API call end-to-end. Removed before the final build.
app.MapGet("/apitest", async (RoadmapService svc) =>
{
    var answers = new AssessmentAnswers
    {
        Profession = "Physician", QualificationCountry = "Egypt", CompletedInternship = "Yes", YearsExperience = "8+ years",
        Location = "Outside Canada", Country = "Egypt", TargetProvince = "Ontario", PlanningToImmigrate = "Yes",
        LicensingStarted = "Yes", ExamsCompleted = new() { "MCCQE Part I", "IELTS" }, RegisteredWithBody = "No",
        CompletedLanguageTest = "Yes", LanguageTest = "IELTS", LanguageScore = "7.5",
        Goals = new() { "Obtain professional licence", "Practise in Canada" },
        LearningNeeds = new() { "Canadian Healthcare System", "Exam Preparation" },
        AdditionalInfo = "MBBCh Cairo 2014, internal medicine. Aiming for Ontario."
    };
    var r = await svc.GenerateAsync(answers);
    return Results.Text(
        $"success={r.Success}\nerror={r.ErrorMessage}\nmodel={r.Model}\nparsedOk={r.ParsedOk}\n" +
        $"pathway={r.Roadmap?.RecommendedPathway}\ntimeline={r.Roadmap?.EstimatedTotalTimeline}\ncost={r.Roadmap?.EstimatedTotalCost}\n" +
        $"phases={r.Roadmap?.Phases.Count}\ninputTokens={r.Usage.InputTokens} outputTokens={r.Usage.OutputTokens} apiCostUsd={r.CostUsd:0.0000}\n\n=== RAW ===\n{r.RawText}");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
