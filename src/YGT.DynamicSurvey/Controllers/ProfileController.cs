using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YGT.DynamicSurvey.Models.Identity;
using YGT.DynamicSurvey.Models.ViewModels.Profile;

namespace YGT.DynamicSurvey.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;


    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager =
            userManager;

        _signInManager =
            signInManager;
    }


    // =====================================================
    // PROFİL - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user =
            await _userManager.GetUserAsync(
                User
            );


        if (user is null)
        {
            return Challenge();
        }


        var model =
            new ProfileViewModel
            {
                FullName =
                    user.FullName,

                Email =
                    user.Email
                    ?? string.Empty,

                CreatedAt =
                    user.CreatedAt,

                IsActive =
                    user.IsActive,

                ReceiveAnnouncements =
                    user.AnnouncementConsent
            };


        return View(model);
    }


    // =====================================================
    // PROFİL BİLGİLERİNİ GÜNCELLE
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        ProfileViewModel model)
    {
        var user =
            await _userManager.GetUserAsync(
                User
            );


        if (user is null)
        {
            return Challenge();
        }


        // Şifre alanları bu işlemde doğrulanmayacak.
        ModelState.Remove(
            nameof(model.CurrentPassword)
        );

        ModelState.Remove(
            nameof(model.NewPassword)
        );

        ModelState.Remove(
            nameof(model.ConfirmNewPassword)
        );


        if (!ModelState.IsValid)
        {
            model.Email =
                user.Email
                ?? string.Empty;

            model.CreatedAt =
                user.CreatedAt;

            model.IsActive =
                user.IsActive;


            return View(
                "Index",
                model
            );
        }


        user.FullName =
            model.FullName.Trim();


        user.AnnouncementConsent =
            model.ReceiveAnnouncements;


        var result =
            await _userManager.UpdateAsync(
                user
            );


        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description
                );
            }


            model.Email =
                user.Email
                ?? string.Empty;

            model.CreatedAt =
                user.CreatedAt;

            model.IsActive =
                user.IsActive;


            return View(
                "Index",
                model
            );
        }


        await _signInManager.RefreshSignInAsync(
            user
        );


        TempData["ProfileSuccess"] =
            "Profil bilgileriniz başarıyla güncellendi.";


        return RedirectToAction(
            nameof(Index)
        );
    }


    // =====================================================
    // ŞİFRE DEĞİŞTİR
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        ProfileViewModel model)
    {
        var user =
            await _userManager.GetUserAsync(
                User
            );


        if (user is null)
        {
            return Challenge();
        }


        // Profil alanlarını bu işlemde doğrulamıyoruz.
        ModelState.Remove(
            nameof(model.FullName)
        );


        if (string.IsNullOrWhiteSpace(
                model.CurrentPassword))
        {
            ModelState.AddModelError(
                nameof(model.CurrentPassword),
                "Mevcut şifrenizi giriniz."
            );
        }


        if (string.IsNullOrWhiteSpace(
                model.NewPassword))
        {
            ModelState.AddModelError(
                nameof(model.NewPassword),
                "Yeni şifrenizi giriniz."
            );
        }


        if (string.IsNullOrWhiteSpace(
                model.ConfirmNewPassword))
        {
            ModelState.AddModelError(
                nameof(model.ConfirmNewPassword),
                "Yeni şifrenizi tekrar giriniz."
            );
        }


        if (!ModelState.IsValid)
        {
            FillProfileInformation(
                model,
                user
            );


            return View(
                "Index",
                model
            );
        }


        var result =
            await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword!,
                model.NewPassword!
            );


        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    TranslateIdentityError(
                        error.Code,
                        error.Description
                    )
                );
            }


            FillProfileInformation(
                model,
                user
            );


            return View(
                "Index",
                model
            );
        }


        await _signInManager.RefreshSignInAsync(
            user
        );


        TempData["PasswordSuccess"] =
            "Şifreniz başarıyla değiştirildi.";


        return RedirectToAction(
            nameof(Index)
        );
    }


    // =====================================================
    // PROFİL BİLGİLERİNİ MODEL'E TEKRAR DOLDUR
    // =====================================================

    private static void FillProfileInformation(
        ProfileViewModel model,
        ApplicationUser user)
    {
        model.FullName =
            user.FullName;

        model.Email =
            user.Email
            ?? string.Empty;

        model.CreatedAt =
            user.CreatedAt;

        model.IsActive =
            user.IsActive;

        model.ReceiveAnnouncements =
            user.AnnouncementConsent;
    }


    // =====================================================
    // IDENTITY HATA MESAJLARI
    // =====================================================

    private static string TranslateIdentityError(
        string code,
        string defaultMessage)
    {
        return code switch
        {
            "PasswordMismatch" =>
                "Mevcut şifreniz hatalı.",

            "PasswordTooShort" =>
                "Yeni şifre yeterince uzun değildir.",

            "PasswordRequiresNonAlphanumeric" =>
                "Yeni şifre en az bir özel karakter içermelidir.",

            "PasswordRequiresDigit" =>
                "Yeni şifre en az bir rakam içermelidir.",

            "PasswordRequiresUpper" =>
                "Yeni şifre en az bir büyük harf içermelidir.",

            "PasswordRequiresLower" =>
                "Yeni şifre en az bir küçük harf içermelidir.",

            _ =>
                defaultMessage
        };
    }
}