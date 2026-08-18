using YGT.DynamicSurvey.Models;

namespace YGT.DynamicSurvey.Models.ViewModels.Survey;

public class ResultsIndexViewModel
{
    public List<SurveyResultListItemViewModel> Surveys { get; set; } = new();
}

public class SurveyResultListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ResponseCount { get; set; }
}

public class SurveyResultsViewModel
{
    public int SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalResponses { get; set; }
    public int MemberResponses { get; set; }
    public int AnonymousResponses { get; set; }
    public int EligibleUserCount { get; set; }
    public double ParticipationRate { get; set; }
    public string MostCommonAnswer { get; set; } = "-";
    public double? OverallRatingAverage { get; set; }
    public List<QuestionResultViewModel> Questions { get; set; } = new();
}

public class QuestionResultViewModel
{
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public int TotalAnswers { get; set; }
    public double? AverageValue { get; set; }
    public double? MinimumValue { get; set; }
    public double? MaximumValue { get; set; }
    public List<OptionResultViewModel> Options { get; set; } = new();
    public List<string> TextAnswers { get; set; } = new();
}

public class OptionResultViewModel
{
    public int OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
