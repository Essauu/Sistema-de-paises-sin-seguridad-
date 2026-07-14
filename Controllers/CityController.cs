using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PaisApp.Data;
using PaisApp.Models;

namespace PaisApp.Controllers;

[Authorize(Policy = "Lectura")]
public class CityController : Controller
{
    private readonly PaisesContext _contexto;

    public CityController(PaisesContext context)
    {
        _contexto = context;
    }

    public async Task<IActionResult> Index(string search, string countryCode, int page = 1, int pageSize = 20)
    {
        var query = _contexto.City.Include(c => c.Country).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Name.Contains(search) || c.District.Contains(search));
        }

        if (!string.IsNullOrEmpty(countryCode))
        {
            query = query.Where(c => c.CountryCode == countryCode);
        }

        var totalElementos = await query.CountAsync();
        var ciudades = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.CountryCode = countryCode;
        ViewBag.Countries = new SelectList(await _contexto.Country.OrderBy(c => c.Name).ToListAsync(), "Code", "Name", countryCode);
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalElementos / pageSize);
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalElementos;

        return View(ciudades);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var ciudad = await _contexto.City
            .Include(c => c.Country)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (ciudad == null) return NotFound();

        return View(ciudad);
    }

    [Authorize(Policy = "Escritura")]
    public async Task<IActionResult> Create(string? countryCode)
    {
        ViewBag.Countries = new SelectList(await _contexto.Country.OrderBy(c => c.Name).ToListAsync(), "Code", "Name", countryCode);
        return View();
    }

    [HttpPost]
    [Authorize(Policy = "Escritura")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,CountryCode,District,Population")] City city)
    {
        if (ModelState.IsValid)
        {
            _contexto.Add(city);
            await _contexto.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Countries = new SelectList(await _contexto.Country.OrderBy(c => c.Name).ToListAsync(), "Code", "Name", city.CountryCode);
        return View(city);
    }

    [Authorize(Policy = "Escritura")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var ciudad = await _contexto.City.FindAsync(id);
        if (ciudad == null) return NotFound();

        ViewBag.Countries = new SelectList(await _contexto.Country.OrderBy(c => c.Name).ToListAsync(), "Code", "Name", ciudad.CountryCode);
        return View(ciudad);
    }

    [HttpPost]
    [Authorize(Policy = "Escritura")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,CountryCode,District,Population")] City city)
    {
        if (id != city.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _contexto.Update(city);
                await _contexto.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CityExists(city.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Countries = new SelectList(await _contexto.Country.OrderBy(c => c.Name).ToListAsync(), "Code", "Name", city.CountryCode);
        return View(city);
    }

    [Authorize(Policy = "Escritura")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var ciudad = await _contexto.City
            .Include(c => c.Country)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (ciudad == null) return NotFound();

        return View(ciudad);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Policy = "Escritura")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ciudad = await _contexto.City.FindAsync(id);
        if (ciudad != null)
        {
            _contexto.City.Remove(ciudad);
            await _contexto.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool CityExists(int id) => _contexto.City.Any(e => e.Id == id);
}
