using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models;
using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EmailService _emailService;

    public NotificationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        EmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task CreateSurveyPublishedNotificationAsync(
        Survey survey,
        string? creatorUserId = null)
    {
        var notification = new Notification
        {
            Title = "Yeni aktif anket",
            Message = $"“{survey.Title}” anketi yayına alındı. Şimdi katılabilirsin.",
            Url = $"/Survey/Join?code={survey.Code}",
            Type = "Survey",
            SurveyId = survey.Id,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var activeUserIds = await _userManager.Users
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .ToListAsync();

        var userNotifications = activeUserIds
            .Where(id => id != creatorUserId)
            .Select(id => new UserNotification
            {
                NotificationId = notification.Id,
                UserId = id,
                IsRead = false
            })
            .ToList();

        if (userNotifications.Count > 0)
        {
            _context.UserNotifications.AddRange(userNotifications);
            await _context.SaveChangesAsync();
        }

        var emailRecipients = await _userManager.Users
            .Where(x => x.IsActive && x.AnnouncementConsent && x.Id != creatorUserId)
            .ToListAsync();
        await _emailService.SendSurveyAnnouncementAsync(survey, emailRecipients);
    }
}
