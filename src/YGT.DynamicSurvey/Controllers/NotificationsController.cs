using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _users;
    public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> users) => (_context, _users) = (context, users);

    [HttpGet]
    public async Task<IActionResult> Open(int id)
    {
        var userId = _users.GetUserId(User);
        var item = await _context.UserNotifications.Include(x => x.Notification)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is null) return RedirectToAction("Index", "Announcement");
        if (!item.IsRead) { item.IsRead = true; item.ReadAt = DateTime.UtcNow; await _context.SaveChangesAsync(); }
        return LocalRedirect(string.IsNullOrWhiteSpace(item.Notification.Url) ? "/Announcement" : item.Notification.Url);
    }
}
