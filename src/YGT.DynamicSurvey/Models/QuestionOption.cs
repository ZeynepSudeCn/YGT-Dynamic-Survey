using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YGT.DynamicSurvey.Models;

public class QuestionOption
{
    public int Id { get; set; }

    [Required]
    public int QuestionId { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = null!;

    [Required(ErrorMessage = "Seçenek metni zorunludur.")]
    [MaxLength(250)]
    public string Text { get; set; } = string.Empty;

    public int Order { get; set; }
}