using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Models.ViewModels.Admin;

public class AdminUsersViewModel
{
    public List<ApplicationUser> Users { get; set; }
        = new();

    public string? Search { get; set; }

    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int PassiveUsers { get; set; }

    public int PendingAdminRequests { get; set; }
}