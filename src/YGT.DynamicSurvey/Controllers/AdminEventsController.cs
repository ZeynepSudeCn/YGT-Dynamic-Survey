using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models;
using YGT.DynamicSurvey.Models.Identity;
using YGT.DynamicSurvey.Services;

namespace YGT.DynamicSurvey.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminEventsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _users;
    private readonly EmailService _email;
    public AdminEventsController(ApplicationDbContext context, UserManager<ApplicationUser> users, EmailService email)
        => (_context, _users, _email) = (context, users, email);

    public async Task<IActionResult> Index() => View(await _context.Events.AsNoTracking().OrderByDescending(x => x.StartsAt).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View(new Event { StartsAt = DateTime.Now.AddDays(1), EndsAt = DateTime.Now.AddDays(1).AddHours(2) });

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.Events.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Event model, List<IFormFile>? photos)
    {
        if (id != model.Id) return BadRequest();
        var item = await _context.Events.FindAsync(id);
        if (item is null) return NotFound();
        if (model.EndsAt <= model.StartsAt)
            ModelState.AddModelError(nameof(model.EndsAt), "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
        if (!ModelState.IsValid) return View(model);

        var imageUrls = await CollectImageUrlsAsync(model.ImageUrl, photos);
        if (!ModelState.IsValid) return View(model);

        item.Title = model.Title;
        item.Category = model.Category;
        item.Summary = model.Summary;
        item.Description = model.Description;
        item.StartsAt = model.StartsAt;
        item.EndsAt = model.EndsAt;
        item.Location = model.Location;
        item.ImageUrl = string.Join('|', imageUrls.Distinct());
        item.InstagramUrl = model.InstagramUrl;
        item.SurveyId = model.SurveyId;
        item.IsPublished = model.IsPublished;

        var notification = await _context.Notifications.FirstOrDefaultAsync(x => x.EventId == id);
        if (notification is not null)
        {
            notification.Title = "Etkinlik güncellendi: " + item.Title;
            notification.Message = item.Summary;
            notification.Url = $"/Announcement/Detail/{item.Id}";
        }
        await _context.SaveChangesAsync();
        TempData["Success"] = "Etkinlik başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Events.FindAsync(id);
        if (item is null) return NotFound();

        var notifications = await _context.Notifications.Where(x => x.EventId == id).ToListAsync();
        _context.Notifications.RemoveRange(notifications);
        _context.Events.Remove(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"“{item.Title}” etkinliği ve bağlı bildirimleri silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Event model, List<IFormFile>? photos)
    {
        if (model.EndsAt <= model.StartsAt)
            ModelState.AddModelError(nameof(model.EndsAt), "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
        if (!ModelState.IsValid) return View(model);

        var imageUrls = await CollectImageUrlsAsync(model.ImageUrl, photos);
        if (!ModelState.IsValid) return View(model);

        model.ImageUrl = string.Join('|', imageUrls.Distinct());
        model.CreatedAt = DateTime.UtcNow;
        _context.Events.Add(model);
        await _context.SaveChangesAsync(); // Etkinlik her koşulda önce kalıcı hale gelir.

        var notification = new Notification
        {
            Title = "Yeni etkinlik: " + model.Title,
            Message = model.Summary,
            Url = $"/Announcement/Detail/{model.Id}",
            Type = "Event", EventId = model.Id, CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var recipients = await _users.Users.Where(x => x.IsActive).ToListAsync();
        _context.UserNotifications.AddRange(recipients.Select(x => new UserNotification { NotificationId = notification.Id, UserId = x.Id }));
        await _context.SaveChangesAsync();

        var sentEmailCount = await _email.SendEventAnnouncementAsync(model, recipients.Where(x => x.AnnouncementConsent));
        TempData["Success"] = $"Etkinlik yayımlandı, {recipients.Count} site bildirimi oluşturuldu ve {sentEmailCount} e-posta gönderildi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<string>> CollectImageUrlsAsync(string? current, List<IFormFile>? photos)
    {
        var urls = (current ?? string.Empty).Split(new[] { '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (photos is null) return urls;
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "events");
        Directory.CreateDirectory(root);
        foreach (var photo in photos.Where(x => x.Length > 0))
        {
            var extension = Path.GetExtension(photo.FileName);
            if (!allowed.Contains(extension) || photo.Length > 8 * 1024 * 1024)
            {
                ModelState.AddModelError("photos", "Fotoğraflar JPG, PNG veya WEBP formatında ve en fazla 8 MB olmalıdır.");
                continue;
            }
            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            await using var stream = System.IO.File.Create(Path.Combine(root, fileName));
            await photo.CopyToAsync(stream);
            urls.Add($"/uploads/events/{fileName}");
        }
        return urls;
    }
}
