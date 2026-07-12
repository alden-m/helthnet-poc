namespace FindMyPath.Poc.Models;

/// <summary>
/// Every answer in the Find My Path questionnaire. One plain class, all state in memory.
/// Nullable strings = "not answered yet"; List&lt;string&gt; = multi-select groups.
/// </summary>
public class AssessmentAnswers
{
    public const string QuestionnaireVersion = "1.0";

    // Section 1 - Professional Background
    public string? Profession { get; set; }
    public string? QualificationCountry { get; set; }
    public string? CompletedInternship { get; set; }   // "Yes" / "No"
    public string? YearsExperience { get; set; }

    // Section 2 - Current Location
    public string? Location { get; set; }              // "Canada" / "Outside Canada"
    public string? Province { get; set; }              // shown only if Canada
    public string? City { get; set; }                  // shown only if Canada
    public string? Country { get; set; }               // shown only if Outside Canada
    public string? TargetProvince { get; set; }        // always (folded in from the client screenshots)

    // Section 3 - Immigration Status
    public string? ImmigrationStatus { get; set; }     // shown only if living in Canada
    public string? PlanningToImmigrate { get; set; }   // shown only if living outside Canada

    // Section 4 - Licensing Status
    public string? LicensingStarted { get; set; }      // "Yes" / "No"
    public List<string> ExamsCompleted { get; set; } = new();
    public string? RegisteredWithBody { get; set; }    // "Yes" / "No"

    // Section 5 - Language Proficiency
    public string? CompletedLanguageTest { get; set; } // "Yes" / "No"
    public string? LanguageTest { get; set; }          // shown only if Yes
    public string? LanguageScore { get; set; }         // shown only if Yes

    // Section 6 - Career Goals
    public List<string> Goals { get; set; } = new();

    // Section 7 - Learning Needs
    public List<string> LearningNeeds { get; set; } = new();

    // Section 8 - Additional Information
    public string? AdditionalInfo { get; set; }

    private bool LivingInCanada => string.Equals(Location, "Canada", StringComparison.OrdinalIgnoreCase);

    /// <summary>Clears answers that are no longer visible so stale values never reach the AI.</summary>
    public void ClearHiddenLocationAnswers()
    {
        if (LivingInCanada)
        {
            Country = null;
            PlanningToImmigrate = null;
        }
        else
        {
            Province = null;
            City = null;
            ImmigrationStatus = null;
        }
    }

    public void ClearHiddenLanguageAnswers()
    {
        if (!string.Equals(CompletedLanguageTest, "Yes", StringComparison.OrdinalIgnoreCase))
        {
            LanguageTest = null;
            LanguageScore = null;
        }
    }
}
