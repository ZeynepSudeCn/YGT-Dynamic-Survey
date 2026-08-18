using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Models.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsSuperAdmin { get; set; }


    public int TotalUsers { get; set; }

    public int TotalSurveys { get; set; }

    public int ActiveSurveys { get; set; }

    public int TotalResponses { get; set; }


    public List<ApplicationUser> PendingAdminRequests { get; set; }
        = new();


    public List<ApplicationUser> ActiveAdmins { get; set; }
        = new();
}