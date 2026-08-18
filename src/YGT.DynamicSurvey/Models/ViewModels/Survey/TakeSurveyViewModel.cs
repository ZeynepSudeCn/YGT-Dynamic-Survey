using YGT.DynamicSurvey.Models;

namespace YGT.DynamicSurvey.Models.ViewModels.Survey;

public class TakeSurveyViewModel
{
    public int SurveyId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<TakeSurveyQuestionViewModel> Questions { get; set; } = new();

    public List<SurveyAnswerInputViewModel> Answers { get; set; } = new();
}


public class TakeSurveyQuestionViewModel
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; }

    public bool IsRequired { get; set; }

    public int Order { get; set; }

    public int? RatingMaxValue { get; set; }

    public List<TakeSurveyOptionViewModel> Options { get; set; } = new();


    // =====================================================
    // DİNAMİK GÖRÜNÜRLÜK
    // =====================================================

    public int? DependsOnQuestionOrder { get; set; }

    public BranchConditionOperator? ConditionOperator { get; set; }

    public string? ShowWhenAnswerEquals { get; set; }
}


public class TakeSurveyOptionViewModel
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public int Order { get; set; }
}


public class SurveyAnswerInputViewModel
{
    public int QuestionId { get; set; }

    public string? Value { get; set; }

    public List<int> SelectedOptionIds { get; set; } = new();
}