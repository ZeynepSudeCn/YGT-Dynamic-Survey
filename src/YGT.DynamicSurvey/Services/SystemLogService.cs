using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models;
using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Services;

public class SystemLogService
{
    private readonly ApplicationDbContext _context;

    public SystemLogService(
        ApplicationDbContext context)
    {
        _context = context;
    }


    // =====================================================
    // SİSTEM KAYDI OLUŞTUR
    // =====================================================

    public async Task LogAsync(
        string action,
        string description,
        string category = "System",
        ApplicationUser? user = null)
    {
        var log =
            new SystemLog
            {
                Action =
                    action,

                Description =
                    description,

                Category =
                    category,

                UserId =
                    user?.Id,

                UserFullName =
                    user?.FullName,

                UserEmail =
                    user?.Email,

                CreatedAt =
                    DateTime.UtcNow
            };


        _context.SystemLogs.Add(log);

        await _context.SaveChangesAsync();
    }
}