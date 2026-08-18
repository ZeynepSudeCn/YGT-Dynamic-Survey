using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YGT.DynamicSurvey.Data;
using YGT.DynamicSurvey.Models.ViewModels;

namespace YGT.DynamicSurvey.Controllers;

[Authorize]
public class AnnouncementController : Controller
{
    private readonly ApplicationDbContext _context;
    public AnnouncementController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var items = await _context.Events.AsNoTracking().Include(x => x.Survey)
            .Where(x => x.IsPublished).OrderBy(x => x.StartsAt).ToListAsync();
        return View(new AnnouncementViewModel
        {
            Live = items.Where(x => x.StartsAt <= now && x.EndsAt >= now).ToList(),
            Upcoming = items.Where(x => x.StartsAt > now).ToList(),
            Past = items.Where(x => x.EndsAt < now).OrderByDescending(x => x.EndsAt).ToList(),
            Featured = items.Where(x => x.EndsAt < now).OrderByDescending(x => x.EndsAt).Take(3).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var item = await _context.Events.AsNoTracking().Include(x => x.Survey)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);
        return item is null ? NotFound() : View(item);
    }
}
