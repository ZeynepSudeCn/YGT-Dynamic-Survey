using System.ComponentModel.DataAnnotations;

namespace YGT.DynamicSurvey.Models;

public class SystemLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(450)]
    public string? UserId { get; set; }

    [StringLength(100)]
    public string? UserFullName { get; set; }

    [StringLength(256)]
    public string? UserEmail { get; set; }

    [StringLength(50)]
    public string Category { get; set; } = "System";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}