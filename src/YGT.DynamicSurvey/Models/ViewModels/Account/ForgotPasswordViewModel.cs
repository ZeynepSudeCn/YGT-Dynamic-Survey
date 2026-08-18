using System.ComponentModel.DataAnnotations;

namespace YGT.DynamicSurvey.Models.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "E-posta alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;
}
