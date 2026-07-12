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
        Q("What is your healthcare profession?", a.Profession);
        Q("In which country did you receive your professional qualification?", a.QualificationCountry);
        Q("Have you completed your internship or residency (if applicable)?", a.CompletedInternship);
        Q("How many years of clinical experience do you have?", a.YearsExperience);
        sb.AppendLine();

        sb.AppendLine("## Current Location");
        Q("Where are you currently living?", a.Location);
        if (string.Equals(a.Location, "Canada", StringComparison.OrdinalIgnoreCase))
        {
            Q("If Canada: Province", a.Province);
            Q("If Canada: City", a.City);
        }
        else if (string.Equals(a.Location, "Outside Canada", StringComparison.OrdinalIgnoreCase))
        {
            Q("If Outside Canada: Country", a.Country);
        }
        Q("Which province do you plan to practise in? (POC enhancement)", a.TargetProvince);
        sb.AppendLine();

        sb.AppendLine("## Immigration Status");
        if (string.Equals(a.Location, "Canada", StringComparison.OrdinalIgnoreCase))
            Q("What is your current immigration status?", a.ImmigrationStatus);
        else if (string.Equals(a.Location, "Outside Canada", StringComparison.OrdinalIgnoreCase))
            Q("Are you planning to immigrate to Canada?", a.PlanningToImmigrate);
        sb.AppendLine();

        sb.AppendLine("## Licensing Status");
        Q("Have you started your licensing process?", a.LicensingStarted);
        QList("Which licensing exams have you completed?", a.ExamsCompleted);
        Q("Are you currently registered with any Canadian regulatory body?", a.RegisteredWithBody);
        sb.AppendLine();

        sb.AppendLine("## Language Proficiency");
        Q("Have you completed an English language test?", a.CompletedLanguageTest);
        if (string.Equals(a.CompletedLanguageTest, "Yes", StringComparison.OrdinalIgnoreCase))
        {
            Q("Which test? (if Yes)", a.LanguageTest);
            Q("Score (if Yes)", a.LanguageScore);
        }
        sb.AppendLine();

        sb.AppendLine("## Career Goals");
        QList("What is your primary goal?", a.Goals);
        sb.AppendLine();

        sb.AppendLine("## Learning Needs");
        QList("Which areas would you like support with?", a.LearningNeeds);
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(a.AdditionalInfo))
        {
            sb.AppendLine("## Additional Information");
            Q("Is there anything else you would like HealthNet to know to better personalise your learning experience?",
                a.AdditionalInfo.Trim());
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
