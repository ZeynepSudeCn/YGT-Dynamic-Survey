using System.ComponentModel.DataAnnotations;

namespace YGT.DynamicSurvey.Models.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;


    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;


    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;


    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(Password),
        ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Şifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;


    [Display(Name = "KVKK Aydınlatma Metni'ni okudum.")]
    public bool HasReadKvkkNotice { get; set; }


    [Display(Name = "Kullanım Koşulları'nı okudum ve kabul ediyorum.")]
    public bool AcceptedTerms { get; set; }


    [Display(
        Name = "Etkinlik, eğitim, gezi ve topluluk duyuruları hakkında e-posta almak istiyorum.")]
    public bool AnnouncementConsent { get; set; }


    [Required(ErrorMessage = "Hesap türünü seçmelisiniz.")]
    public string AccountType { get; set; } = "User";
}