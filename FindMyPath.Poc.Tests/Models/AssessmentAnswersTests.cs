using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Tests.Models;

public class AssessmentAnswersTests
{
    [Fact]
    public void QuestionnaireContractHasAnExplicitVersion()
    {
        Assert.Equal("1.0", AssessmentAnswers.QuestionnaireVersion);
    }

    [Theory]
    [InlineData("Canada")]
    [InlineData("cAnAdA")]
    public void ClearHiddenLocationAnswersForCanadaClearsOutsideCanadaAnswers(string location)
    {
        var answers = CompleteLocationAnswers();
        answers.Location = location;

        answers.ClearHiddenLocationAnswers();

        Assert.Equal("Ontario", answers.Province);
        Assert.Equal("Toronto", answers.City);
        Assert.Equal("PR", answers.ImmigrationStatus);
        Assert.Equal("Ontario", answers.TargetProvince);
        Assert.Null(answers.Country);
        Assert.Null(answers.PlanningToImmigrate);
    }

    [Theory]
    [InlineData("Outside Canada")]
    [InlineData("")]
    [InlineData(null)]
    public void ClearHiddenLocationAnswersOutsideCanadaClearsCanadaAnswers(string? location)
    {
        var answers = CompleteLocationAnswers();
        answers.Location = location;

        answers.ClearHiddenLocationAnswers();

        Assert.Equal("Egypt", answers.Country);
        Assert.Equal("Yes", answers.PlanningToImmigrate);
        Assert.Equal("Ontario", answers.TargetProvince);
        Assert.Null(answers.Province);
        Assert.Null(answers.City);
        Assert.Null(answers.ImmigrationStatus);
    }

    [Theory]
    [InlineData("Yes")]
    [InlineData("yEs")]
    public void ClearHiddenLanguageAnswersPreservesTestDetailsWhenCompleted(string completed)
    {
        var answers = new AssessmentAnswers
        {
            CompletedLanguageTest = completed,
            LanguageTest = "IELTS",
            LanguageScore = "7.5",
        };

        answers.ClearHiddenLanguageAnswers();

        Assert.Equal("IELTS", answers.LanguageTest);
        Assert.Equal("7.5", answers.LanguageScore);
    }

    [Theory]
    [InlineData("No")]
    [InlineData("Undecided")]
    [InlineData("")]
    [InlineData(null)]
    public void ClearHiddenLanguageAnswersClearsTestDetailsUnlessCompleted(string? completed)
    {
        var answers = new AssessmentAnswers
        {
            CompletedLanguageTest = completed,
            LanguageTest = "IELTS",
            LanguageScore = "7.5",
        };

        answers.ClearHiddenLanguageAnswers();

        Assert.Null(answers.LanguageTest);
        Assert.Null(answers.LanguageScore);
    }

    private static AssessmentAnswers CompleteLocationAnswers() => new()
    {
        Location = "Canada",
        Province = "Ontario",
        City = "Toronto",
        Country = "Egypt",
        TargetProvince = "Ontario",
        ImmigrationStatus = "PR",
        PlanningToImmigrate = "Yes",
    };
}
