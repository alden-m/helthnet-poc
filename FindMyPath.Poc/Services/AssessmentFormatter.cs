using System.Text;
using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>Serializes the (visible, answered) questionnaire into a readable Q&amp;A block for the AI.</summary>
public static class AssessmentFormatter
{
    public static string ToUserMessage(AssessmentAnswers a)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Below are an internationally educated health professional's answers to the HealthNet \"Find My Path\" intake questionnaire. Draft their personalised Canadian licensing roadmap.");
        sb.AppendLine();

        void Q(string q, string? val)
        {
            if (!string.IsNullOrWhiteSpace(val)) sb.AppendLine($"- {q}: {val}");
        }
        void QList(string q, List<string> vals)
        {
            if (vals.Count > 0) sb.AppendLine($"- {q}: {string.Join(", ", vals)}");
        }

        sb.AppendLine("## Professional Background");
        Q("Healthcare profession", a.Profession);
        Q("Country of qualification", a.QualificationCountry);
        Q("Completed internship/residency", a.CompletedInternship);
        Q("Years of clinical experience", a.YearsExperience);
        sb.AppendLine();

        sb.AppendLine("## Current Location");
        Q("Currently living", a.Location);
        Q("Province", a.Province);
        Q("City", a.City);
        Q("Country", a.Country);
        Q("Province they plan to practise in", a.TargetProvince);
        sb.AppendLine();

        sb.AppendLine("## Immigration Status");
        Q("Current immigration status", a.ImmigrationStatus);
        Q("Planning to immigrate to Canada", a.PlanningToImmigrate);
        sb.AppendLine();

        sb.AppendLine("## Licensing Status");
        Q("Started licensing process", a.LicensingStarted);
        QList("Licensing exams completed", a.ExamsCompleted);
        Q("Registered with a Canadian regulatory body", a.RegisteredWithBody);
        sb.AppendLine();

        sb.AppendLine("## Language Proficiency");
        Q("Completed an English language test", a.CompletedLanguageTest);
        Q("Test", a.LanguageTest);
        Q("Score", a.LanguageScore);
        sb.AppendLine();

        sb.AppendLine("## Career Goals");
        QList("Primary goals", a.Goals);
        sb.AppendLine();

        sb.AppendLine("## Learning Needs");
        QList("Areas they want support with", a.LearningNeeds);
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(a.AdditionalInfo))
        {
            sb.AppendLine("## Additional Information");
            sb.AppendLine(a.AdditionalInfo.Trim());
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
