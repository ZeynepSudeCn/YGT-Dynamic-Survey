namespace YGT.DynamicSurvey.Models.ViewModels;

public class AnnouncementViewModel
{
    public List<Event> Live { get; init; } = new();
    public List<Event> Upcoming { get; init; } = new();
    public List<Event> Past { get; init; } = new();
    public List<Event> Featured { get; init; } = new();
}
