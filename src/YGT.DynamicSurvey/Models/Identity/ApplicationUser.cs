using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace YGT.DynamicSurvey.Models.Identity;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;


    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;


    public bool IsActive { get; set; }
        = true;


    // =====================================================
    // DUYURU E-POSTASI TERCİHİ
    // =====================================================

    public bool AnnouncementConsent { get; set; }
        = false;


    // =====================================================
    // YÖNETİCİ BAŞVURU BİLGİLERİ
    // =====================================================

    public bool RequestedAdminAccess { get; set; }
        = false;


    public string AdminRequestStatus { get; set; }
        = "None";


    public DateTime? AdminRequestedAt { get; set; }


    public DateTime? AdminReviewedAt { get; set; }


    public string? AdminReviewedByUserId { get; set; }
}