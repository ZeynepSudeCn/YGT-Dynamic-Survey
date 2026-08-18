using System.ComponentModel.DataAnnotations;

namespace YGT.DynamicSurvey.Models;

public class Survey
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Anket başlığı zorunludur.")]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Anketin başlayacağı tarih
    [Required]
    public DateTime StartDate { get; set; }

    // Anketin biteceği tarih
    [Required]
    public DateTime EndDate { get; set; }

    // Anketi oluşturan kullanıcı
    public string? CreatedByUserId { get; set; }

    // Ankete ait sorular
    public ICollection<Question> Questions { get; set; }
        = new List<Question>();
}