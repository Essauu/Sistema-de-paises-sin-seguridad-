using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisApp.Data;
using PaisApp.Models;

namespace PaisApp.Controllers;

[Authorize(Policy = "Lectura")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _registrador;
    private readonly PaisesContext _contexto;

    public HomeController(ILogger<HomeController> logger, PaisesContext context)
    {
        _registrador = logger;
        _contexto = context;
    }

    public async Task<IActionResult> Index()
    {
        var estadisticas = new
        {
            TotalCountries = await _contexto.Country.CountAsync(),
            TotalCities = await _contexto.City.CountAsync(),
            TotalLanguages = await _contexto.CountryLanguage.CountAsync(),
            TotalPopulation = await _contexto.Country.SumAsync(c => (long?)c.Population) ?? 0,
            TopPopulated = await _contexto.Country.OrderByDescending(c => c.Population).Take(5).ToListAsync(),
        };

        ViewBag.Stats = estadisticas;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
