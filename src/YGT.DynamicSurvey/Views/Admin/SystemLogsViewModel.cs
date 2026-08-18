using YGT.DynamicSurvey.Models;

namespace YGT.DynamicSurvey.Models.ViewModels.Admin;

public class SystemLogsViewModel
{
    public List<SystemLog> Logs { get; set; } = new();

    public string? Search { get; set; }

    public string? Category { get; set; }

    public int TotalLogs { get; set; }

    public int TodayLogs { get; set; }

    public int UserLogs { get; set; }

    public int SurveyLogs { get; set; }

    public int AdminLogs { get; set; }
}