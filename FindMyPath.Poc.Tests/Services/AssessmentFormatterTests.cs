using FindMyPath.Poc.Models;
using FindMyPath.Poc.Services;

namespace FindMyPath.Poc.Tests.Services;

public class AssessmentFormatterTests
{
    [Fact]
    public void ToUserMessageUsesFullPrdQuestionWordingInQuestionnaireOrder()
    {
        var answers = new AssessmentAnswers
        {
            Profession = "Physician",
            QualificationCountry = "Egypt",
            CompletedInternship = "Yes",
            YearsExperience = "8+ years",
            Location = "Canada",
            Province = "Ontario",
            City = "Toronto",
            TargetProvince = "Ontario",
            ImmigrationStatus = "PR",
            LicensingStarted = "Yes",
            ExamsCompleted = ["MCCQE Part I", "IELTS"],
            RegisteredWithBody = "No",
            CompletedLanguageTest = "Yes",
            LanguageTest = "IELTS",
            LanguageScore = "7.5",
            Goals = ["Obtain professional licence", "Practise in Canada"],
            LearningNeeds = ["Canadian Healthcare System", "Exam Preparation"],
            AdditionalInfo = "I am aiming to settle in Ontario.",
        };

        var message = AssessmentFormatter.ToUserMessage(answers);

        AssertLinesInOrder(
            message,
            "- What is your healthcare profession?: Physician",
            "- In which country did you receive your professional qualification?: Egypt",
            "- Have you completed your internship or residency (if applicable)?: Yes",
            "- How many years of clinical experience do you have?: 8+ years",
            "- Where are you currently living?: Canada",
            "- If Canada: Province: Ontario",
            "- If Canada: City: Toronto",
            "- Which province do you plan to practise in? (POC enhancement): Ontario",
            "- What is your current immigration status?: PR",
            "- Have you started your licensing process?: Yes",
            "- Which licensing exams have you completed?: MCCQE Part I, IELTS",
            "- Are you currently registered with any Canadian regulatory body?: No",
            "- Have you completed an English language test?: Yes",
            "- Which test? (if Yes): IELTS",
            "- Score (if Yes): 7.5",
            "- What is your primary goal?: Obtain professional licence, Practise in Canada",
            "- Which areas would you like support with?: Canadian Healthcare System, Exam Preparation",
            "- Is there anything else you would like HealthNet to know to better personalise your learning experience?: I am aiming to settle in Ontario.");

        Assert.DoesNotContain("- If Outside Canada: Country:", message);
        Assert.DoesNotContain("- Are you planning to immigrate to Canada?:", message);
    }

    [Fact]
    public void ToUserMessageUsesOutsideCanadaQuestionsAndSuppressesStaleCanadaAnswers()
    {
        var answers = new AssessmentAnswers
        {
            Location = "Outside Canada",
            Country = "India",
            PlanningToImmigrate = "Undecided",
            TargetProvince = "Not sure yet",
            Province = "Ontario",
            City = "Toronto",
            ImmigrationStatus = "Work Permit",
        };

        var message = AssessmentFormatter.ToUserMessage(answers);

        AssertLinesInOrder(
            message,
            "- Where are you currently living?: Outside Canada",
            "- If Outside Canada: Country: India",
            "- Which province do you plan to practise in? (POC enhancement): Not sure yet",
            "- Are you planning to immigrate to Canada?: Undecided");

        Assert.DoesNotContain("- If Canada: Province:", message);
        Assert.DoesNotContain("- If Canada: City:", message);
        Assert.DoesNotContain("- What is your current immigration status?:", message);
    }

    [Fact]
    public void ToUserMessageOmitsHiddenLanguageDetailsAndEveryEmptyAnswer()
    {
        var answers = new AssessmentAnswers
        {
            Profession = "Nurse",
            QualificationCountry = "   ",
            Location = "Canada",
            Province = "Ontario",
            Country = "Philippines", // stale and hidden
            PlanningToImmigrate = "Yes", // stale and hidden
            CompletedLanguageTest = "No",
            LanguageTest = "CELBAN", // stale and hidden
            LanguageScore = "Advanced", // stale and hidden
            ExamsCompleted = [],
            Goals = [],
            LearningNeeds = [],
            AdditionalInfo = " \t ",
        };

        var message = AssessmentFormatter.ToUserMessage(answers);

        Assert.Contains("- What is your healthcare profession?: Nurse", message);
        Assert.Contains("- Have you completed an English language test?: No", message);
        Assert.DoesNotContain("- In which country did you receive your professional qualification?:", message);
        Assert.DoesNotContain("- If Outside Canada: Country:", message);
        Assert.DoesNotContain("- Are you planning to immigrate to Canada?:", message);
        Assert.DoesNotContain("- Which test? (if Yes):", message);
        Assert.DoesNotContain("- Score (if Yes):", message);
        Assert.DoesNotContain("- Which licensing exams have you completed?:", message);
        Assert.DoesNotContain("- What is your primary goal?:", message);
        Assert.DoesNotContain("- Which areas would you like support with?:", message);
        Assert.DoesNotContain("- Is there anything else you would like HealthNet to know", message);
    }

    private static void AssertLinesInOrder(string text, params string[] expectedLines)
    {
        var previous = -1;
        foreach (var line in expectedLines)
        {
            var index = text.IndexOf(line, StringComparison.Ordinal);
            Assert.True(index >= 0, $"Expected line was not present: {line}\n\nActual message:\n{text}");
            Assert.True(index > previous, $"Expected line was out of order: {line}\n\nActual message:\n{text}");
            previous = index;
        }
    }
}
