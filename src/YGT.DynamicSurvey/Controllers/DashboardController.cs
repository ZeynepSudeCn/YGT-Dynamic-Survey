using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }


    // =====================================================
    // KULLANICI ANA PANELİ
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user =
            await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }


        // =================================================
        // ADMIN / SUPERADMIN NORMAL KULLANICI PANELİNE GİRMESİN
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
            return RedirectToAction(
                "Index",
                "Admin"
            );
        }


        var now =
            DateTime.Now;


        // =================================================
        // ŞU ANDA KATILIMA AÇIK ANKETLER
        // =================================================

        var openSurveys =
            await _context.Surveys
                .Where(x =>
                    x.IsActive &&
                    (x.StartDate == default || x.StartDate <= now) &&
                    (x.EndDate == default || x.EndDate >= now)
                )
                .OrderBy(x =>
                    x.EndDate)
                .ToListAsync();

        var participatedSurveyIds = await _context.SurveyResponses
            .Where(x => x.UserId == user.Id)
            .Select(x => x.SurveyId)
            .Distinct()
            .ToListAsync();

        var waitingSurveys = openSurveys
            .Where(x => !participatedSurveyIds.Contains(x.Id))
            .Take(3)
            .ToList();

        var nextEvent = await _context.Events
            .Where(x => x.IsPublished && x.StartsAt > now)
            .OrderBy(x => x.StartsAt)
            .FirstOrDefaultAsync();

        var lastResponse = await _context.SurveyResponses
            .Include(x => x.Survey)
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync();

        var unreadNotifications = await _context.UserNotifications
            .CountAsync(x => x.UserId == user.Id && !x.IsRead && x.Notification.IsActive);

        var upcomingEventCount = await _context.Events
            .CountAsync(x => x.IsPublished && x.StartsAt > now);


        // =================================================
        // PANEL BİLGİLERİ
        // =================================================

        ViewBag.FullName =
            user.FullName;

        ViewBag.Email =
            user.Email;

        ViewBag.OpenSurveyCount =
            openSurveys.Count;

        ViewBag.OpenSurveys =
            openSurveys
                .Take(6)
                .ToList();

        ViewBag.WaitingSurveys = waitingSurveys;
        ViewBag.NextEvent = nextEvent;
        ViewBag.LastResponse = lastResponse;
        ViewBag.UpcomingEventCount = upcomingEventCount;
        ViewBag.UnreadNotificationCount = unreadNotifications;


        /*
            Katıldığım anketler özelliğini
            birazdan kullanıcı ile SurveyResponse'u
            bağlayarak gerçek hale getireceğiz.

            Şimdilik 0 gösteriyoruz.
        */

        ViewBag.ParticipatedSurveyCount = participatedSurveyIds.Count;

        ViewBag.AnnouncementCount = unreadNotifications;


        return View();
    }
}
