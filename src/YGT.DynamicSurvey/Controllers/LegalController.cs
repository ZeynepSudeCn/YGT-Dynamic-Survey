using Microsoft.AspNetCore.Mvc;

namespace YGT.DynamicSurvey.Controllers;

public class LegalController : Controller
{
    [HttpGet]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet]
    public IActionResult KvkkNotice()
    {
        return View();
    }

    [HttpGet]
    public IActionResult TermsOfUse()
    {
        return View();
    }

    [HttpGet]
    public IActionResult PrivacyPolicy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult CookiePolicy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }
}