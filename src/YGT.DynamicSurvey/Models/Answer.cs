using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YGT.DynamicSurvey.Models;

public class Answer
{
    public int Id { get; set; }

    [Required]
    public int SurveyResponseId { get; set; }

    [ForeignKey(nameof(SurveyResponseId))]
    public SurveyResponse SurveyResponse { get; set; } = null!;

    [Required]
    public int QuestionId { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = null!;

    // Metin, sayı, rating, likert vb. cevaplar burada tutulabilir.
    public string? Value { get; set; }

    // Çoklu seçim için seçilen option Id'leri:
    // örn: "3,5,8"
    public string? SelectedOptionIds { get; set; }
}