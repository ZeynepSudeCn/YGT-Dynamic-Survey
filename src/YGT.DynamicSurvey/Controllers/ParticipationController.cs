using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models.Identity;

namespace YGT.DynamicSurvey.Controllers;

[Authorize]
public class ParticipationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ParticipationController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> OpenSurveys()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var now = DateTime.Now;

        var participatedSurveyIds =
            await _context.SurveyResponses
                .Where(x => x.UserId == user.Id)
                .Select(x => x.SurveyId)
                .Distinct()
                .ToListAsync();

        var surveys =
            await _context.Surveys
                .Where(x =>
                    x.IsActive &&
                    (x.StartDate == default || x.StartDate <= now) &&
                    (x.EndDate == default || x.EndDate >= now) &&
                    !participatedSurveyIds.Contains(x.Id))
                .OrderBy(x => x.EndDate)
                .ToListAsync();

        return View(surveys);
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var responses =
            await _context.SurveyResponses
                .Include(x => x.Survey)
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.SubmittedAt)
                .ToListAsync();

        return View(responses);
    }
}
