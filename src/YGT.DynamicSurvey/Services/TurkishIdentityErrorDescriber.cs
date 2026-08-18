using Microsoft.AspNetCore.Identity;

namespace YGT.DynamicSurvey.Services;

public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "İşlem tamamlanamadı. Lütfen tekrar deneyin.");
    public override IdentityError ConcurrencyFailure() => Error(nameof(ConcurrencyFailure), "Bilgiler başka bir işlem tarafından değiştirildi. Lütfen tekrar deneyin.");
    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "Mevcut şifreniz hatalı.");
    public override IdentityError InvalidToken() => Error(nameof(InvalidToken), "Geçersiz veya süresi dolmuş bağlantı.");
    public override IdentityError LoginAlreadyAssociated() => Error(nameof(LoginAlreadyAssociated), "Bu giriş başka bir hesapla ilişkilendirilmiş.");
    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), "Kullanıcı adı geçersiz.");
    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), "Geçerli bir e-posta adresi girin.");
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), "Bu kullanıcı adı zaten kullanılıyor.");
    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), "Bu e-posta adresi zaten kullanılıyor.");
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"Şifre en az {length} karakter olmalıdır.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "Şifre en az bir özel karakter içermelidir.");
    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "Şifre en az bir rakam içermelidir.");
    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "Şifre en az bir küçük harf içermelidir.");
    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "Şifre en az bir büyük harf içermelidir.");
    public override IdentityError UserAlreadyHasPassword() => Error(nameof(UserAlreadyHasPassword), "Bu hesabın zaten bir şifresi var.");
    public override IdentityError UserLockoutNotEnabled() => Error(nameof(UserLockoutNotEnabled), "Bu hesap için kilitleme etkin değil.");
    public override IdentityError UserAlreadyInRole(string role) => Error(nameof(UserAlreadyInRole), "Kullanıcı bu role zaten sahip.");
    public override IdentityError UserNotInRole(string role) => Error(nameof(UserNotInRole), "Kullanıcı bu role sahip değil.");
}
