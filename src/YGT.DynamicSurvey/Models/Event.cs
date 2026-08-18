using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YGT.DynamicSurvey.Models;

public class Event
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık alanı zorunludur.")]
    [StringLength(160, ErrorMessage = "Başlık en fazla 160 karakter olabilir.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kısa açıklama alanı zorunludur.")]
    [StringLength(300, ErrorMessage = "Kısa açıklama en fazla 300 karakter olabilir.")]
    [Display(Name = "Kısa açıklama")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama alanı zorunludur.")]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori alanı zorunludur.")]
    [StringLength(60)]
    [Display(Name = "Kategori")]
    public string Category { get; set; } = "Etkinlik";

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    [Display(Name = "Başlangıç tarihi")]
    public DateTime StartsAt { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur.")]
    [Display(Name = "Bitiş tarihi")]
    public DateTime EndsAt { get; set; }

    [StringLength(200)]
    [Display(Name = "Konum")]
    public string? Location { get; set; }

    [Display(Name = "Görsel adresi")]
    public string? ImageUrl { get; set; }

    [NotMapped]
    public IReadOnlyList<string> ImageUrls => (ImageUrl ?? string.Empty)
        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [StringLength(500)]
    [Display(Name = "Instagram bağlantısı")]
    public string? InstagramUrl { get; set; }

    [Display(Name = "Değerlendirme anketi")]
    public int? SurveyId { get; set; }
    public Survey? Survey { get; set; }

    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
