using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Models;

public class SurveyResponse
{
    public int Id { get; set; }

    public int SurveyId { get; set; }

    public Survey Survey { get; set; } = null!;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}