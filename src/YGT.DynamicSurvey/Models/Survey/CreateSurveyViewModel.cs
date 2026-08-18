using System.ComponentModel.DataAnnotations;
using YGT.DynamicSurvey.Models;

namespace YGT.DynamicSurvey.Models.ViewModels.Survey;

public class CreateSurveyViewModel
{
    [Required(ErrorMessage = "Anket başlığı zorunludur.")]
    [StringLength(
        150,
        ErrorMessage = "Anket başlığı en fazla 150 karakter olabilir."
    )]
    [Display(Name = "Anket Başlığı")]
    public string Title { get; set; } = string.Empty;


    [Required(ErrorMessage = "Anket açıklaması zorunludur.")]
    [StringLength(
        1000,
        ErrorMessage = "Açıklama en fazla 1000 karakter olabilir."
    )]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;


    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [Display(Name = "Başlangıç Tarihi")]
    public DateTime? StartDate { get; set; }


    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [Display(Name = "Bitiş Tarihi")]
    public DateTime? EndDate { get; set; }


    public List<CreateQuestionViewModel> Questions { get; set; } = new();
}


public class CreateQuestionViewModel
{
    [Required(ErrorMessage = "Soru metni zorunludur.")]
    [StringLength(
        1000,
        ErrorMessage = "Soru metni en fazla 1000 karakter olabilir."
    )]
    public string Text { get; set; } = string.Empty;


    public QuestionType Type { get; set; } =
        QuestionType.SingleChoice;


    public bool IsRequired { get; set; }


    public int Order { get; set; }


    public int? RatingMaxValue { get; set; } = 5;


    public List<string> Options { get; set; } = new();


    // =====================================================
    // DİNAMİK ANKET
    // =====================================================

    public int? DependsOnQuestionOrder { get; set; }

    public BranchConditionOperator? ConditionOperator { get; set; }


    // Mevcut DB alanını koruyoruz.
    // Artık yalnızca "koşul değeri" olarak kullanılıyor.
    //
    // Örn:
    // operator = LessThanOrEqual
    // value = "2"
    public string? ShowWhenAnswerEquals { get; set; }
}