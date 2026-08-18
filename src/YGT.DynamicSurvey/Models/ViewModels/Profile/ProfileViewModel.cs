using System.ComponentModel.DataAnnotations;

namespace YGT.DynamicSurvey.Models.ViewModels.Profile;

public class ProfileViewModel
{
    // =====================================================
    // PROFİL BİLGİLERİ
    // =====================================================

    [Required(
        ErrorMessage = "Ad soyad zorunludur."
    )]
    [StringLength(
        100,
        ErrorMessage = "Ad soyad en fazla 100 karakter olabilir."
    )]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; }
        = string.Empty;


    [Display(Name = "E-posta")]
    public string Email { get; set; }
        = string.Empty;


    [Display(Name = "Hesap Oluşturma Tarihi")]
    public DateTime CreatedAt { get; set; }


    [Display(Name = "Hesap Durumu")]
    public bool IsActive { get; set; }


    [Display(
        Name = "Etkinlik, eğitim, gezi ve topluluk duyurularını e-posta ile almak istiyorum."
    )]
    public bool ReceiveAnnouncements { get; set; }


    // =====================================================
    // ŞİFRE DEĞİŞTİRME
    // =====================================================

    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string? CurrentPassword { get; set; }


    [DataType(DataType.Password)]
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessage = "Yeni şifre en az 6 karakter olmalıdır."
    )]
    [Display(Name = "Yeni Şifre")]
    public string? NewPassword { get; set; }


    [DataType(DataType.Password)]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "Yeni şifreler eşleşmiyor."
    )]
    [Display(Name = "Yeni Şifre Tekrar")]
    public string? ConfirmNewPassword { get; set; }
}