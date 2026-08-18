namespace YGT.DynamicSurvey.Models;

public class Notification
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string Type { get; set; } = "Survey";

    public int? SurveyId { get; set; }

    public int? EventId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public List<UserNotification> UserNotifications { get; set; }
        = new();
}
