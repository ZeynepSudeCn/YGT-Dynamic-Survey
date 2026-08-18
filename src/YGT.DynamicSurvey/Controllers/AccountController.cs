using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models;
using YGT.DynamicSurvey.Models.Identity;
using YGT.DynamicSurvey.Models.ViewModels.Account;
using YGT.DynamicSurvey.Services;

namespace YGT.DynamicSurvey.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly SystemLogService _systemLogService;
    private readonly EmailService _emailService;
    private readonly ApplicationDbContext _context;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        SystemLogService systemLogService,
        EmailService emailService,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _systemLogService = systemLogService;
        _emailService = emailService;
        _context = context;
    }


    // =====================================================
    // KAYIT OL - GET
    // =====================================================

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(
                "Index",
                "Dashboard"
            );
        }

        return View(
            new RegisterViewModel
            {
                AccountType = "User"
            }
        );
    }


    // =====================================================
    // KAYIT OL - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model)
    {
        // =================================================
        // HESAP TÜRÜ
        // =================================================

        if (
            model.AccountType != "User" &&
            model.AccountType != "AdminRequest"
        )
        {
            ModelState.AddModelError(
                nameof(model.AccountType),
                "Geçerli bir hesap türü seçiniz."
            );
        }


        // =================================================
        // KVKK
        // =================================================

        if (!model.HasReadKvkkNotice)
        {
            ModelState.AddModelError(
                nameof(model.HasReadKvkkNotice),
                "KVKK Aydınlatma Metni'ni okuduğunuzu belirtmelisiniz."
            );
        }


        // =================================================
        // KULLANIM KOŞULLARI
        // =================================================

        if (!model.AcceptedTerms)
        {
            ModelState.AddModelError(
                nameof(model.AcceptedTerms),
                "Kullanım Koşulları'nı kabul etmelisiniz."
            );
        }


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var email =
            model.Email.Trim();


        var existingUser =
            await _userManager.FindByEmailAsync(
                email
            );


        if (existingUser is not null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Bu e-posta adresi zaten kullanılıyor."
            );

            return View(model);
        }


        var isAdminRequest =
            model.AccountType == "AdminRequest";


        var user =
            new ApplicationUser
            {
                UserName =
                    email,

                Email =
                    email,

                FullName =
                    model.FullName.Trim(),

                AnnouncementConsent =
                    model.AnnouncementConsent,

                CreatedAt =
                    DateTime.UtcNow,

                IsActive =
                    true,

                RequestedAdminAccess =
                    isAdminRequest,

                AdminRequestStatus =
                    isAdminRequest
                        ? "Pending"
                        : "None",

                AdminRequestedAt =
                    isAdminRequest
                        ? DateTime.UtcNow
                        : null,

                AdminReviewedAt =
                    null,

                AdminReviewedByUserId =
                    null
            };


        var result =
            await _userManager.CreateAsync(
                user,
                model.Password
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

            return View(model);
        }


        await _systemLogService.LogAsync(
            isAdminRequest
                ? "Yönetici Başvurusu"
                : "Kullanıcı Kaydı",
            isAdminRequest
                ? $"{user.FullName} yönetici hesabı için başvuru oluşturdu."
                : $"{user.FullName} sisteme kayıt oldu.",
            isAdminRequest
                ? "Admin"
                : "User",
            user
        );

        if (isAdminRequest)
        {
            var notification = new Notification
            {
                Title = "Yeni yönetici başvurusu",
                Message = $"{user.FullName} ({user.Email}) yönetici olmak için başvurdu.",
                Url = "/Admin/Users?status=Pending",
                Type = "AdminApplication",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var managerIds = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdminIds = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            var recipients = managerIds.Concat(superAdminIds)
                .Where(x => x.IsActive)
                .Select(x => x.Id)
                .Distinct()
                .Select(id => new UserNotification
                {
                    NotificationId = notification.Id,
                    UserId = id,
                    IsRead = false
                })
                .ToList();

            if (recipients.Count > 0)
            {
                _context.UserNotifications.AddRange(recipients);
                await _context.SaveChangesAsync();
            }

            await _emailService.SendAdminApplicationAsync(
                user,
                managerIds.Concat(superAdminIds));
        }


        // =================================================
        // NORMAL KULLANICI
        // =================================================

        if (!isAdminRequest)
        {
            await _signInManager.SignInAsync(
                user,
                isPersistent: false
            );


            return RedirectToAction(
                "Index",
                "Dashboard"
            );
        }


        // =================================================
        // YÖNETİCİ ADAYI
        // =================================================

        TempData["AdminApplicationSuccess"] =
            "Hesabınız oluşturuldu. Yönetici başvurunuz onay bekliyor.";


        return RedirectToAction(
            nameof(AdminApplicationPending)
        );
    }


    // =====================================================
    // YÖNETİCİ BAŞVURU BEKLEME
    // =====================================================

    [HttpGet]
    public IActionResult AdminApplicationPending()
    {
        return View();
    }


    // =====================================================
    // NORMAL GİRİŞ - GET
    // =====================================================

    [HttpGet]
    public IActionResult Login(
        string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(
                "Index",
                "Dashboard"
            );
        }


        ViewData["ReturnUrl"] =
            returnUrl;


        return View(
            new LoginViewModel
            {
                ReturnUrl =
                    returnUrl
            }
        );
    }


    // =====================================================
    // NORMAL GİRİŞ - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        returnUrl ??=
            model.ReturnUrl;


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var user =
            await _userManager.FindByEmailAsync(
                model.Email
            );


        if (user is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "E-posta veya şifre hatalı."
            );

            return View(model);
        }


        if (!user.IsActive)
        {
            ModelState.AddModelError(
                string.Empty,
                "Bu kullanıcı hesabı aktif değildir."
            );

            return View(model);
        }


        // =================================================
        // YÖNETİCİ BAŞVURUSU BEKLİYOR
        // =================================================

        if (
            user.RequestedAdminAccess &&
            user.AdminRequestStatus == "Pending"
        )
        {
            ModelState.AddModelError(
                string.Empty,
                "Yönetici başvurunuz henüz onaylanmamıştır."
            );

            return View(model);
        }


        // =================================================
        // YÖNETİCİ BAŞVURUSU REDDEDİLDİ
        // =================================================

        if (
            user.RequestedAdminAccess &&
            user.AdminRequestStatus == "Rejected"
        )
        {
            ModelState.AddModelError(
                string.Empty,
                "Yönetici başvurunuz reddedilmiştir."
            );

            return View(model);
        }


        // =================================================
        // ADMIN / SUPERADMIN NORMAL GİRİŞTEN GİREMEZ
        // =================================================

        var isAdmin =
            await _userManager.IsInRoleAsync(
                user,
                "Admin"
            );


        var isSuperAdmin =
            await _userManager.IsInRoleAsync(
                user,
                "SuperAdmin"
            );


        if (isAdmin || isSuperAdmin)
        {
            ModelState.AddModelError(
                string.Empty,
                "Bu hesap yönetici hesabıdır. Lütfen Yönetici Girişi ekranını kullanınız."
            );

            return View(model);
        }


        // =================================================
        // NORMAL KULLANICI GİRİŞİ
        // =================================================

        var result =
            await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true
            );


        if (result.Succeeded)
        {
            await _systemLogService.LogAsync(
                "Kullanıcı Girişi",
                $"{user.FullName} sisteme giriş yaptı.",
                "Authentication",
                user
            );

            if (
                !string.IsNullOrWhiteSpace(
                    returnUrl
                )
                &&
                Url.IsLocalUrl(
                    returnUrl
                )
            )
            {
                return LocalRedirect(
                    returnUrl
                );
            }


            return RedirectToAction(
                "Index",
                "Dashboard"
            );
        }


        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Çok fazla hatalı giriş yapıldı. Lütfen daha sonra tekrar deneyiniz."
            );

            return View(model);
        }


        ModelState.AddModelError(
            string.Empty,
            "E-posta veya şifre hatalı."
        );


        return View(model);
    }


    // =====================================================
    // ŞİFREMİ UNUTTUM
    // =====================================================

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token }, Request.Scheme)!;
            var sent = await _emailService.SendPasswordResetAsync(user, resetUrl);
            if (!sent)
            {
                ModelState.AddModelError(string.Empty, "E-posta şu anda gönderilemedi. Lütfen birkaç dakika sonra tekrar deneyin.");
                return View(model);
            }
        }
        ViewBag.Message = "Bu e-posta kayıtlıysa şifre sıfırlama bağlantısı gönderildi. Gelen kutusu, Spam ve Gereksiz klasörlerini kontrol edin.";
        return View(new ForgotPasswordViewModel());
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token) => View(new ResetPasswordViewModel { Email = email, Token = token });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null) { ModelState.AddModelError(string.Empty, "Şifre sıfırlama bağlantısı geçersiz."); return View(model); }
        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
        TempData["LoginMessage"] = "Şifreniz yenilendi. Yeni şifrenizle giriş yapabilirsiniz.";
        return RedirectToAction(nameof(Login));
    }

    // =====================================================
    // ÇIKIŞ
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user is not null)
        {
            await _systemLogService.LogAsync(
                "Kullanıcı Çıkışı",
                $"{user.FullName} sistemden çıkış yaptı.",
                "Authentication",
                user
            );
        }

        await _signInManager.SignOutAsync();

        return RedirectToAction(
            "Index",
            "Home"
        );
    }
}
