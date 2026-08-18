using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models.Identity;
using YGT.DynamicSurvey.Models.ViewModels.Admin;
using YGT.DynamicSurvey.Services;

namespace YGT.DynamicSurvey.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SystemLogService _systemLogService;

    private const string SuperAdminEmail =
        "ygtkku@gmail.com";

    private const string InitialAdminEmail =
        "z.sudecengiz@gmail.com";


    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        SystemLogService systemLogService)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _systemLogService = systemLogService;
    }


    // =====================================================
    // YÖNETİCİ GİRİŞİ - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser is not null)
            {
                var isAdmin =
                    await _userManager.IsInRoleAsync(
                        currentUser,
                        "Admin"
                    );

                var isSuperAdmin =
                    await _userManager.IsInRoleAsync(
                        currentUser,
                        "SuperAdmin"
                    );

                if (isAdmin || isSuperAdmin)
                {
                    return RedirectToAction(
                        nameof(Index)
                    );
                }
            }

            await _signInManager.SignOutAsync();
        }

        return View(
            new AdminLoginViewModel()
        );
    }


    // =====================================================
    // YÖNETİCİ GİRİŞİ - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        AdminLoginViewModel model)
    {
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
                "Bu hesap aktif değildir."
            );

            return View(model);
        }

        await EnsureRolesAsync();

        var normalizedEmail =
            user.Email?
                .Trim()
                .ToLowerInvariant();


        // =================================================
        // ANA TOPLULUK HESABI = SUPERADMIN
        // =================================================

        if (normalizedEmail == SuperAdminEmail)
        {
            if (!await _userManager.IsInRoleAsync(
                    user,
                    "SuperAdmin"))
            {
                var roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        "SuperAdmin"
                    );

                if (!roleResult.Succeeded)
                {
                    AddIdentityErrors(
                        roleResult
                    );

                    return View(model);
                }
            }

            user.RequestedAdminAccess =
                true;

            user.AdminRequestStatus =
                "Approved";

            user.AdminReviewedAt =
                DateTime.UtcNow;

            var updateResult =
                await _userManager.UpdateAsync(
                    user
                );

            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(
                    updateResult
                );

                return View(model);
            }
        }


        // =================================================
        // İLK ADMIN HESABI
        // =================================================

        else if (normalizedEmail == InitialAdminEmail)
        {
            if (!await _userManager.IsInRoleAsync(
                    user,
                    "Admin"))
            {
                var roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        "Admin"
                    );

                if (!roleResult.Succeeded)
                {
                    AddIdentityErrors(
                        roleResult
                    );

                    return View(model);
                }
            }

            user.RequestedAdminAccess =
                true;

            user.AdminRequestStatus =
                "Approved";

            user.AdminReviewedAt =
                DateTime.UtcNow;

            var updateResult =
                await _userManager.UpdateAsync(
                    user
                );

            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(
                    updateResult
                );

                return View(model);
            }
        }


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


        // =================================================
        // BAŞVURU BEKLİYOR
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
        // BAŞVURU REDDEDİLDİ
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
        // YÖNETİCİ DEĞİLSE GİRİŞ YOK
        // =================================================

        if (!isAdmin && !isSuperAdmin)
        {
            ModelState.AddModelError(
                string.Empty,
                "Bu hesabın yönetici yetkisi bulunmamaktadır."
            );

            return View(model);
        }


        // =================================================
        // ŞİFRE KONTROLÜ
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
                "Yönetici Girişi",
                $"{user.FullName} yönetici paneline giriş yaptı.",
                "Authentication",
                user
            );

            return RedirectToAction(
                nameof(Index)
            );
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Çok fazla hatalı giriş yapıldı. Hesap geçici olarak kilitlendi."
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
    // YÖNETİM PANELİ
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user =
            await _userManager.GetUserAsync(
                User
            );

        if (user is null)
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                nameof(Login)
            );
        }

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

        if (!isAdmin && !isSuperAdmin)
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                nameof(Login)
            );
        }

        var now =
            DateTime.Now;

        var model =
            new AdminDashboardViewModel
            {
                FullName =
                    string.IsNullOrWhiteSpace(
                        user.FullName
                    )
                        ? "Yönetici"
                        : user.FullName.Trim(),

                Email =
                    user.Email
                    ?? string.Empty,

                IsSuperAdmin =
                    isSuperAdmin,

                TotalUsers =
                    await _userManager.Users
                        .CountAsync(),

                TotalSurveys =
                    await _context.Surveys
                        .CountAsync(),

                ActiveSurveys =
                    await _context.Surveys
                        .CountAsync(x =>
                            x.IsActive &&
                            x.StartDate != default &&
                            x.EndDate != default &&
                            x.StartDate <= now &&
                            x.EndDate >= now
                        ),

                TotalResponses =
                    await _context.SurveyResponses
                        .CountAsync(),

                PendingAdminRequests =
                    new List<ApplicationUser>(),

                ActiveAdmins =
                    new List<ApplicationUser>()
            };

        if (isSuperAdmin)
        {
            model.PendingAdminRequests =
                await _userManager.Users
                    .Where(x =>
                        x.RequestedAdminAccess &&
                        x.AdminRequestStatus == "Pending"
                    )
                    .OrderBy(x =>
                        x.AdminRequestedAt
                    )
                    .ToListAsync();

            var adminUsers =
                await _userManager
                    .GetUsersInRoleAsync(
                        "Admin"
                    );

            model.ActiveAdmins =
                adminUsers
                    .Where(x =>
                        !string.Equals(
                            x.Email,
                            SuperAdminEmail,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .OrderBy(x =>
                        x.FullName
                    )
                    .ToList();
        }

        return View(model);
    }


    // =====================================================
    // KULLANICILAR
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Users(
        string? search)
    {
        var currentUser =
            await _userManager.GetUserAsync(
                User
            );

        if (currentUser is null)
        {
            return Challenge();
        }

        var query =
            _userManager.Users
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search =
                search.Trim();

            query =
                query.Where(x =>
                    x.FullName.Contains(search) ||
                    (
                        x.Email != null &&
                        x.Email.Contains(search)
                    )
                );
        }

        var users =
            await query
                .OrderByDescending(x =>
                    x.CreatedAt)
                .ToListAsync();

        var model =
            new AdminUsersViewModel
            {
                Users =
                    users,

                Search =
                    search,

                TotalUsers =
                    users.Count,

                ActiveUsers =
                    users.Count(x =>
                        x.IsActive),

                PassiveUsers =
                    users.Count(x =>
                        !x.IsActive),

                PendingAdminRequests =
                    users.Count(x =>
                        x.RequestedAdminAccess &&
                        x.AdminRequestStatus == "Pending"
                    )
            };

        return View(model);
    }


    // =====================================================
    // KULLANICI AKTİF / PASİF
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(
        string id)
    {
        var currentUser =
            await _userManager.GetUserAsync(
                User
            );

        if (currentUser is null)
        {
            return Challenge();
        }

        var targetUser =
            await _userManager.FindByIdAsync(
                id
            );

        if (targetUser is null)
        {
            return NotFound();
        }

        if (
            string.Equals(
                targetUser.Email,
                SuperAdminEmail,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            TempData["AdminError"] =
                "Ana yönetici hesabı pasif hale getirilemez.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        if (targetUser.Id == currentUser.Id)
        {
            TempData["AdminError"] =
                "Kendi hesabınızı buradan pasif hale getiremezsiniz.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        targetUser.IsActive =
            !targetUser.IsActive;

        var updateResult =
            await _userManager.UpdateAsync(
                targetUser
            );

        if (!updateResult.Succeeded)
        {
            TempData["AdminError"] =
                "Kullanıcı durumu değiştirilemedi.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        await _systemLogService.LogAsync(
            targetUser.IsActive
                ? "Kullanıcı Aktifleştirildi"
                : "Kullanıcı Pasifleştirildi",
            targetUser.IsActive
                ? $"{targetUser.FullName} hesabı aktif hale getirildi."
                : $"{targetUser.FullName} hesabı pasif hale getirildi.",
            "Admin",
            currentUser
        );

        TempData["AdminSuccess"] =
            targetUser.IsActive
                ? $"{targetUser.FullName} hesabı aktif hale getirildi."
                : $"{targetUser.FullName} hesabı pasif hale getirildi.";

        return RedirectToAction(
            nameof(Users)
        );
    }


    // =====================================================
    // YÖNETİCİ BAŞVURUSUNU ONAYLA
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAdmin(
        string id)
    {
        var reviewer =
            await _userManager.GetUserAsync(
                User
            );

        if (reviewer is null)
        {
            return Challenge();
        }

        var targetUser =
            await _userManager.FindByIdAsync(
                id
            );

        if (targetUser is null)
        {
            return NotFound();
        }

        if (
            !targetUser.RequestedAdminAccess ||
            targetUser.AdminRequestStatus != "Pending"
        )
        {
            TempData["AdminError"] =
                "Bu kullanıcının bekleyen bir yönetici başvurusu bulunmamaktadır.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        await EnsureRolesAsync();

        if (!await _userManager.IsInRoleAsync(
                targetUser,
                "Admin"))
        {
            var roleResult =
                await _userManager.AddToRoleAsync(
                    targetUser,
                    "Admin"
                );

            if (!roleResult.Succeeded)
            {
                TempData["AdminError"] =
                    "Yönetici rolü atanamadı.";

                return RedirectToAction(
                    nameof(Users)
                );
            }
        }

        targetUser.RequestedAdminAccess =
            true;

        targetUser.AdminRequestStatus =
            "Approved";

        targetUser.AdminReviewedAt =
            DateTime.UtcNow;

        targetUser.AdminReviewedByUserId =
            reviewer.Id;

        var updateResult =
            await _userManager.UpdateAsync(
                targetUser
            );

        if (!updateResult.Succeeded)
        {
            TempData["AdminError"] =
                "Yönetici başvurusu güncellenemedi.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        await _systemLogService.LogAsync(
            "Yönetici Onayı",
            $"{targetUser.FullName} yönetici olarak onaylandı.",
            "Admin",
            reviewer
        );

        TempData["AdminSuccess"] =
            $"{targetUser.FullName} yönetici olarak onaylandı.";

        return RedirectToAction(
            nameof(Users)
        );
    }


    // =====================================================
    // YÖNETİCİ BAŞVURUSUNU REDDET
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectAdmin(
        string id)
    {
        var reviewer =
            await _userManager.GetUserAsync(
                User
            );

        if (reviewer is null)
        {
            return Challenge();
        }

        var targetUser =
            await _userManager.FindByIdAsync(
                id
            );

        if (targetUser is null)
        {
            return NotFound();
        }

        if (
            !targetUser.RequestedAdminAccess ||
            targetUser.AdminRequestStatus != "Pending"
        )
        {
            TempData["AdminError"] =
                "Bu kullanıcının bekleyen bir yönetici başvurusu bulunmamaktadır.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        targetUser.AdminRequestStatus =
            "Rejected";

        targetUser.AdminReviewedAt =
            DateTime.UtcNow;

        targetUser.AdminReviewedByUserId =
            reviewer.Id;

        var updateResult =
            await _userManager.UpdateAsync(
                targetUser
            );

        if (!updateResult.Succeeded)
        {
            TempData["AdminError"] =
                "Yönetici başvurusu güncellenemedi.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        await _systemLogService.LogAsync(
            "Yönetici Başvurusu Reddedildi",
            $"{targetUser.FullName} kullanıcısının yönetici başvurusu reddedildi.",
            "Admin",
            reviewer
        );

        TempData["AdminSuccess"] =
            $"{targetUser.FullName} kullanıcısının yönetici başvurusu reddedildi.";

        return RedirectToAction(
            nameof(Users)
        );
    }


    // =====================================================
    // YÖNETİCİ YETKİSİNİ KALDIR
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAdmin(
        string id)
    {
        var reviewer =
            await _userManager.GetUserAsync(
                User
            );

        if (reviewer is null)
        {
            return Challenge();
        }

        var targetUser =
            await _userManager.FindByIdAsync(
                id
            );

        if (targetUser is null)
        {
            return NotFound();
        }

        if (
            string.Equals(
                targetUser.Email,
                SuperAdminEmail,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            TempData["AdminError"] =
                "Ana yönetici hesabının yetkisi kaldırılamaz.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        if (targetUser.Id == reviewer.Id)
        {
            TempData["AdminError"] =
                "Kendi yönetici yetkinizi buradan kaldıramazsınız.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        if (
            await _userManager.IsInRoleAsync(
                targetUser,
                "Admin"
            )
        )
        {
            var removeResult =
                await _userManager.RemoveFromRoleAsync(
                    targetUser,
                    "Admin"
                );

            if (!removeResult.Succeeded)
            {
                TempData["AdminError"] =
                    "Yönetici rolü kaldırılamadı.";

                return RedirectToAction(
                    nameof(Users)
                );
            }
        }

        targetUser.RequestedAdminAccess =
            false;

        targetUser.AdminRequestStatus =
            "None";

        targetUser.AdminReviewedAt =
            DateTime.UtcNow;

        targetUser.AdminReviewedByUserId =
            reviewer.Id;

        var updateResult =
            await _userManager.UpdateAsync(
                targetUser
            );

        if (!updateResult.Succeeded)
        {
            TempData["AdminError"] =
                "Yönetici bilgileri güncellenemedi.";

            return RedirectToAction(
                nameof(Users)
            );
        }

        await _systemLogService.LogAsync(
            "Yönetici Yetkisi Kaldırıldı",
            $"{targetUser.FullName} kullanıcısının yönetici yetkisi kaldırıldı.",
            "Admin",
            reviewer
        );

        TempData["AdminSuccess"] =
            $"{targetUser.FullName} artık yönetici değildir.";

        return RedirectToAction(
            nameof(Users)
        );
    }



    // =====================================================
    // SIDEBAR UYUMLULUK SAYFALARI
    // =====================================================

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> SuperAdminCenter()
    {
        var users = _userManager.Users;
        var admins = await _userManager.GetUsersInRoleAsync("Admin");

        ViewBag.TotalUsers = await users.CountAsync();
        ViewBag.ActiveUsers = await users.CountAsync(x => x.IsActive);
        ViewBag.PassiveUsers = await users.CountAsync(x => !x.IsActive);
        ViewBag.PendingAdmins = await users.CountAsync(x =>
            x.RequestedAdminAccess && x.AdminRequestStatus == "Pending");
        ViewBag.AdminCount = admins.Count;
        ViewBag.TotalSurveys = await _context.Surveys.CountAsync();
        ViewBag.TotalResponses = await _context.SurveyResponses.CountAsync();

        return View();
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public IActionResult Surveys()
    {
        return RedirectToAction(
            "Index",
            "Survey"
        );
    }


    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Notifications()
    {
        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(x => x.Type == "AdminApplication" && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync();
        return View(notifications);
    }


    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public IActionResult Settings()
    {
        return View();
    }


    // =====================================================
    // SİSTEM KAYITLARI
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Logs(
        string? search,
        string? category)
    {
        var query =
            _context.SystemLogs
                .AsNoTracking()
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query =
                query.Where(x =>
                    x.Action.Contains(search) ||
                    x.Description.Contains(search) ||
                    (
                        x.UserFullName != null &&
                        x.UserFullName.Contains(search)
                    ) ||
                    (
                        x.UserEmail != null &&
                        x.UserEmail.Contains(search)
                    )
                );
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            category = category.Trim();

            var normalizedCategory =
                category.ToLowerInvariant() switch
                {
                    "authentication" => "Authentication",
                    "security" => "Authentication",
                    "guvenlik" => "Authentication",
                    "güvenlik" => "Authentication",
                    "survey" => "Survey",
                    "anket" => "Survey",
                    "admin" => "Admin",
                    "yonetici" => "Admin",
                    "yönetici" => "Admin",
                    "user" => "User",
                    "kullanici" => "User",
                    "kullanıcı" => "User",
                    "system" => "System",
                    "sistem" => "System",
                    _ => category
                };

            category = normalizedCategory;

            query =
                query.Where(x =>
                    x.Category == normalizedCategory
                );
        }

        var logs =
            await query
                .OrderByDescending(x =>
                    x.CreatedAt)
                .Take(500)
                .ToListAsync();

        var today =
            DateTime.UtcNow.Date;

        var model =
            new SystemLogsViewModel
            {
                Logs =
                    logs,

                Search =
                    search,

                Category =
                    category,

                TotalLogs =
                    await _context.SystemLogs
                        .CountAsync(),

                TodayLogs =
                    await _context.SystemLogs
                        .CountAsync(x =>
                            x.CreatedAt >= today
                        ),

                UserLogs =
                    await _context.SystemLogs
                        .CountAsync(x =>
                            x.Category == "User"
                        ),

                SurveyLogs =
                    await _context.SystemLogs
                        .CountAsync(x =>
                            x.Category == "Survey"
                        ),

                AdminLogs =
                    await _context.SystemLogs
                        .CountAsync(x =>
                            x.Category == "Admin"
                        )
            };

        return View(model);
    }


    // =====================================================
    // YÖNETİCİ ÇIKIŞI
    // =====================================================

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user is not null)
        {
            await _systemLogService.LogAsync(
                "Yönetici Çıkışı",
                $"{user.FullName} yönetici panelinden çıkış yaptı.",
                "Authentication",
                user
            );
        }

        await _signInManager.SignOutAsync();

        return RedirectToAction(
            nameof(Login)
        );
    }


    // =====================================================
    // GEREKLİ ROLLERİ OLUŞTUR
    // =====================================================

    private async Task EnsureRolesAsync()
    {
        if (!await _roleManager.RoleExistsAsync(
                "Admin"
            ))
        {
            await _roleManager.CreateAsync(
                new IdentityRole(
                    "Admin"
                )
            );
        }

        if (!await _roleManager.RoleExistsAsync(
                "SuperAdmin"
            ))
        {
            await _roleManager.CreateAsync(
                new IdentityRole(
                    "SuperAdmin"
                )
            );
        }
    }


    // =====================================================
    // IDENTITY HATALARI
    // =====================================================

    private void AddIdentityErrors(
        IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description
            );
        }
    }
}
