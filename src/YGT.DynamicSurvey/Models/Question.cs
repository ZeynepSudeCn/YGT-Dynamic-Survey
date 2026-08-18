namespace YGT.DynamicSurvey.Models;

// =====================================================
// SORU TİPLERİ
// =====================================================
public enum QuestionType
{
    SingleChoice = 1,
    MultipleChoice = 2,
    ShortText = 3,
    LongText = 4,
    Rating = 5,
    Likert = 6,
    Number = 7,
    YesNo = 8
}


// =====================================================
// DİNAMİK KOŞUL OPERATÖRLERİ
// =====================================================
public enum BranchConditionOperator
{
    Equals = 1,
    NotEquals = 2,
    LessThan = 3,
    LessThanOrEqual = 4,
    GreaterThan = 5,
    GreaterThanOrEqual = 6,
    Contains = 7,
    Answered = 8,
    NotAnswered = 9
}


public class Question
{
    public int Id { get; set; }

    public int SurveyId { get; set; }

    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; }

    public bool IsRequired { get; set; }

    public int Order { get; set; }

    public int? RatingMaxValue { get; set; }


    // =====================================================
    // DİNAMİK / KOŞULLU SORU
    //
    // DependsOnQuestionOrder = null
    // => Her zaman göster.
    //
    // Örnek:
    // DependsOnQuestionOrder = 1
    // ConditionOperator = LessThanOrEqual
    // ShowWhenAnswerEquals = "2"
    //
    // => 1. sorunun cevabı 2 veya daha düşükse göster.
    //
    // NOT:
    // ShowWhenAnswerEquals alanının adını mevcut migration'ı
    // bozmamak için koruyoruz. Artık "koşul değeri" anlamında
    // kullanılıyor.
    // =====================================================

    public int? DependsOnQuestionOrder { get; set; }

    public BranchConditionOperator? ConditionOperator { get; set; }

    public string? ShowWhenAnswerEquals { get; set; }


    public Survey Survey { get; set; } = null!;

    public List<QuestionOption> Options { get; set; } = new();
}