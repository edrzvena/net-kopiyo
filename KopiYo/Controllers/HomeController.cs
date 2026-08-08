using System.Diagnostics;
using KopiYo.Common;
using KopiYo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers;

public class HomeController : Controller
{
    /// <summary>
    /// Beranda berbeda per role: Admin butuh ringkasan bisnis, Kasir butuh
    /// langsung layar jualan tanpa satu klik pun terbuang.
    /// </summary>
    public IActionResult Index()
        => User.IsAdmin()
            ? RedirectToAction("Dashboard", "Reports")
            : RedirectToAction("Index", "Pos");

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
