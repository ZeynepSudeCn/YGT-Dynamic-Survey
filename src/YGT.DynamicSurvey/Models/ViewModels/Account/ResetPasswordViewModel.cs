using System.ComponentModel.DataAnnotations;

namespace YGT.DynamicSurvey.Models.ViewModels.Account;

public class ResetPasswordViewModel
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni şifre")]
    public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Yeni şifre tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
