using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using YGT.DynamicSurvey.Models;

namespace YGT.DynamicSurvey.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ILogger<HomeController> logger)
    {
        _logger = logger;
    }


    // =====================================================
    // ANA SAYFA
    // =====================================================

    public IActionResult Index()
    {
        // Kullanıcı giriş yaptıysa anonim ana sayfayı
        // göstermiyoruz. Direkt Dashboard'a gönderiyoruz.
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(
                "Index",
                "Dashboard"
            );
        }

        return View();
    }


    // =====================================================
    // GİZLİLİK
    // =====================================================

    public IActionResult Privacy()
    {
        return View();
    }


    // =====================================================
    // HATA SAYFASI
    // =====================================================

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            }
        );
    }
}