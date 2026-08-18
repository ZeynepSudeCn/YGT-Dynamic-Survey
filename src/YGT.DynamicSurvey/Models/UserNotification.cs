using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Models;

public class UserNotification
{
    public int Id { get; set; }

    public int NotificationId { get; set; }

    public Notification Notification { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }
}