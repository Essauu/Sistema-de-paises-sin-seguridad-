using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PaisApp.Data;
using PaisApp.Models;

namespace PaisApp.Controllers;

[Authorize(Policy = "Lectura")]
public class CountryLanguageController : Controller
{
    private readonly PaisesContext _contexto;

    public CountryLanguageController(PaisesContext context)
    {
        _contexto = context;
    }

    public async Task<IActionResult> Index(string search, string countryCode, int page = 1, int pageSize = 20)
    {
        var query = _contexto.CountryLanguage
            .Include(cl => cl.Country)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(cl => cl.Language.Contains(search) || cl.Country.Name.Contains(search));
        }

        if (!string.IsNullOrEmpty(countryCode))
        {
            query = query.Where(cl => cl.CountryCode == countryCode);
        }

        var totalElementos = await query.CountAsync();
        var idiomas = await query
            .OrderBy(cl => cl.Country.Name)
            .ThenBy(cl => cl.Language)
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

        return View(idiomas);
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
    public async Task<IActionResult> Create([Bind("CountryCode,Language,IsOfficial,Percentage")] CountryLanguage countryLanguage)
    {
        if (ModelState.IsValid)
        {
            var existe = await _contexto.CountryLanguage
                .AnyAsync(cl => cl.CountryCode == countryLanguage.CountryCode && cl.Language == countryLanguage.Language);

            if (existe)
            {
                ModelState.AddModelError("Language", "Este idioma ya existe para el país seleccionado.");
            }
            else
            {
                _contexto.Add(countryLanguage);
                await _contexto.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
        }
        ViewBag.Countries = new SelectList(await _contexto.Country.OrderBy(c => c.Name).ToListAsync(), "Code", "Name", countryLanguage.CountryCode);
        return View(countryLanguage);
    }

    [Authorize(Policy = "Escritura")]
    public async Task<IActionResult> Edit(string countryCode, string language)
    {
        if (string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(language))
            return NotFound();

        var countryLanguage = await _contexto.CountryLanguage
            .FirstOrDefaultAsync(cl => cl.CountryCode == countryCode && cl.Language == language);

        if (countryLanguage == null) return NotFound();

        ViewBag.Countries = new SelectList(await _contexto.Country.OrderBy(c => c.Name).ToListAsync(), "Code", "Name", countryLanguage.CountryCode);
        return View(countryLanguage);
    }

    [HttpPost]
    [Authorize(Policy = "Escritura")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string countryCode, string language, [Bind("CountryCode,Language,IsOfficial,Percentage")] CountryLanguage countryLanguage)
    {
        if (countryCode != countryLanguage.CountryCode || language != countryLanguage.Language)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _contexto.Update(countryLanguage);
                await _contexto.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CountryLanguageExists(countryLanguage.CountryCode, countryLanguage.Language))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Countries = new SelectList(await _contexto.Country.OrderBy(c => c.Name).ToListAsync(), "Code", "Name", countryLanguage.CountryCode);
        return View(countryLanguage);
    }

    [Authorize(Policy = "Escritura")]
    public async Task<IActionResult> Delete(string countryCode, string language)
    {
        if (string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(language))
            return NotFound();

        var countryLanguage = await _contexto.CountryLanguage
            .Include(cl => cl.Country)
            .FirstOrDefaultAsync(cl => cl.CountryCode == countryCode && cl.Language == language);

        if (countryLanguage == null) return NotFound();

        return View(countryLanguage);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Policy = "Escritura")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string countryCode, string language)
    {
        var countryLanguage = await _contexto.CountryLanguage
            .FirstOrDefaultAsync(cl => cl.CountryCode == countryCode && cl.Language == language);

        if (countryLanguage != null)
        {
            _contexto.CountryLanguage.Remove(countryLanguage);
            await _contexto.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool CountryLanguageExists(string countryCode, string language) 
        => _contexto.CountryLanguage.Any(e => e.CountryCode == countryCode && e.Language == language);
}
