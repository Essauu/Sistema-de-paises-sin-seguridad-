using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using PaisApp.Data;
using PaisApp.Models;
using PaisApp.Services;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace PaisApp.Controllers;

[Authorize(Policy = "Lectura")]
public class CountryController : Controller
{
    private readonly PaisesContext _contexto;
    private readonly IFileValidationService _fileValidation;
    private readonly IPdfReportService _pdfReport;
    private readonly IWebHostEnvironment _entorno;

    public CountryController(PaisesContext context, IFileValidationService fileValidation, IPdfReportService pdfReport, IWebHostEnvironment env)
    {
        _contexto = context;
        _fileValidation = fileValidation;
        _pdfReport = pdfReport;
        _entorno = env;
    }

    public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 20)
    {
        var query = _contexto.Country.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search) || c.Code.Contains(search) || c.Code2.Contains(search));

        var totalElementos = await query.CountAsync();
        var paises = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalElementos / pageSize);
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalElementos;

        return View(paises);
    }

    public async Task<IActionResult> Details(string id)
    {
        if (id == null) return NotFound();

        var pais = await _contexto.Country
            .Include(c => c.CapitalCity)
            .Include(c => c.CountryLanguages)
            .Include(c => c.Cities)
            .FirstOrDefaultAsync(m => m.Code == id);

        if (pais == null) return NotFound();

        return View(pais);
    }

    [Authorize(Policy = "Escritura")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Continents = GetContinents();
        ViewBag.Cities = new SelectList(await _contexto.City.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost]
    [Authorize(Policy = "Escritura")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Code,Name,Continent,Region,SurfaceArea,IndepYear,Population,LifeExpectancy,GNP,GNPOld,LocalName,GovernmentForm,HeadOfState,Capital,Code2")] Country pais, IFormFile? archivoBandera)
    {
        if (ModelState.IsValid)
        {
            if (await _contexto.Country.AnyAsync(c => c.Code == pais.Code))
            {
                ModelState.AddModelError("Code", "El código de país ya existe.");
            }
            else
            {
                if (archivoBandera != null)
                {
                    var validation = await _fileValidation.ValidateImageAsync(archivoBandera);
                    if (!validation.IsValid)
                    {
                        ModelState.AddModelError("flagFile", validation.ErrorMessage!);
                        ViewBag.Continents = GetContinents();
                        ViewBag.Cities = new SelectList(await _contexto.City.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
                        return View(pais);
                    }
                    pais.FlagUrl = await SaveFlagAsync(archivoBandera, pais.Code);
                }

                _contexto.Add(pais);
                await _contexto.SaveChangesAsync();
                TempData["Success"] = "País creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        ViewBag.Continents = GetContinents();
        ViewBag.Cities = new SelectList(await _contexto.City.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", pais.Capital);
        return View(pais);
    }

    [Authorize(Policy = "Escritura")]
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null) return NotFound();

        var pais = await _contexto.Country.FindAsync(id);
        if (pais == null) return NotFound();

        ViewBag.Continents = GetContinents();
        ViewBag.Cities = new SelectList(await _contexto.City.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", pais.Capital);
        return View(pais);
    }

    [HttpPost]
    [Authorize(Policy = "Escritura")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Code,Name,Continent,Region,SurfaceArea,IndepYear,Population,LifeExpectancy,GNP,GNPOld,LocalName,GovernmentForm,HeadOfState,Capital,Code2")] Country pais, IFormFile? archivoBandera)
    {
        if (id != pais.Code) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var existente = await _contexto.Country.AsNoTracking().FirstOrDefaultAsync(c => c.Code == id);

                if (archivoBandera != null)
                {
                    var validation = await _fileValidation.ValidateImageAsync(archivoBandera);
                    if (!validation.IsValid)
                    {
                        ModelState.AddModelError("flagFile", validation.ErrorMessage!);
                        ViewBag.Continents = GetContinents();
                        ViewBag.Cities = new SelectList(await _contexto.City.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", pais.Capital);
                        return View(pais);
                    }

                    if (!string.IsNullOrEmpty(existente?.FlagUrl))
                        DeleteFlagFile(existente.FlagUrl);

                    pais.FlagUrl = await SaveFlagAsync(archivoBandera, pais.Code);
                }
                else
                {
                    pais.FlagUrl = existente?.FlagUrl;
                }

                _contexto.Update(pais);
                await _contexto.SaveChangesAsync();
                TempData["Success"] = "País actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CountryExists(pais.Code)) return NotFound();
                throw;
            }
        }

        ViewBag.Continents = GetContinents();
        ViewBag.Cities = new SelectList(await _contexto.City.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", pais.Capital);
        return View(pais);
    }

    [Authorize(Policy = "Escritura")]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null) return NotFound();

        var pais = await _contexto.Country
            .Include(c => c.CapitalCity)
            .FirstOrDefaultAsync(m => m.Code == id);

        if (pais == null) return NotFound();

        return View(pais);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Policy = "Escritura")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var pais = await _contexto.Country.FindAsync(id);
        if (pais != null)
        {
            if (!string.IsNullOrEmpty(pais.FlagUrl))
                DeleteFlagFile(pais.FlagUrl);

            _contexto.Country.Remove(pais);
            await _contexto.SaveChangesAsync();
            TempData["Success"] = "País eliminado.";
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Report(string id)
    {
        if (id == null) return NotFound();

        var pais = await _contexto.Country
            .Include(c => c.CapitalCity)
            .Include(c => c.CountryLanguages)
            .Include(c => c.Cities)
            .FirstOrDefaultAsync(m => m.Code == id);

        if (pais == null) return NotFound();

        return View(pais);
    }

    private static readonly HttpClient _httpClient = new HttpClient();

    [HttpGet]
    public IActionResult SqlSearch(string q)
    {
        if (string.IsNullOrEmpty(q)) return Content("Ingrese un término de búsqueda.");

        var sql = $"SELECT Code, Name, Continent, Population FROM country WHERE Name LIKE '%{q}%' OR Code LIKE '%{q}%'";
        using var cmd = _contexto.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        _contexto.Database.OpenConnection();
        using var reader = cmd.ExecuteReader();

        var results = new List<string>();
        while (reader.Read())
        {
            results.Add($"<tr><td>{reader.GetString(0)}</td><td>{reader.GetString(1)}</td><td>{reader.GetString(2)}</td><td>{reader.GetInt32(3)}</td></tr>");
        }

        var html = $@"<html><body><h2>Resultados para: {q}</h2>
        <table border=1><tr><th>Code</th><th>Name</th><th>Continent</th><th>Population</th></tr>
        {string.Join("", results)}</table></body></html>";

        return Content(html, "text/html");
    }

    public async Task<IActionResult> DownloadReport(string id)
    {
        if (id == null) return NotFound();

        var pais = await _contexto.Country
            .Include(c => c.CapitalCity)
            .Include(c => c.CountryLanguages)
            .Include(c => c.Cities)
            .FirstOrDefaultAsync(m => m.Code == id);

        if (pais == null) return NotFound();

        var bytesBandera = await DownloadFlagAsync(pais.Code2);
        var bytesPdf = _pdfReport.GenerateCountryReport(
            pais,
            pais.CapitalCity,
            pais.CountryLanguages,
            pais.Cities,
            bytesBandera);

        return File(bytesPdf, "application/pdf", $"Reporte_{pais.Code}_{pais.Name}.pdf");
    }

    private static async Task<byte[]?> DownloadFlagAsync(string code2)
    {
        try
        {
            var url = $"https://flagcdn.com/64x48/{code2.ToLower()}.png";
            return await _httpClient.GetByteArrayAsync(url);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> SaveFlagAsync(IFormFile file, string countryCode)
    {
        var directorioSubidas = Path.Combine(_entorno.WebRootPath, "uploads", "flags");
        Directory.CreateDirectory(directorioSubidas);

        var rutaArchivo = Path.Combine(directorioSubidas, file.FileName);

        using var stream = new FileStream(rutaArchivo, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/flags/{file.FileName}";
    }

    private void DeleteFlagFile(string flagUrl)
    {
        var rutaArchivo = Path.Combine(_entorno.WebRootPath, flagUrl.TrimStart('/'));
        if (System.IO.File.Exists(rutaArchivo))
            System.IO.File.Delete(rutaArchivo);
    }

    private byte[]? GetFlagBytes(string? flagUrl)
    {
        if (string.IsNullOrEmpty(flagUrl)) return null;
        var rutaArchivo = Path.Combine(_entorno.WebRootPath, flagUrl.TrimStart('/'));
        return System.IO.File.Exists(rutaArchivo) ? System.IO.File.ReadAllBytes(rutaArchivo) : null;
    }

    private bool CountryExists(string id) => _contexto.Country.Any(e => e.Code == id);

    private static List<string> GetContinents() => new()
    {
        "Asia", "Europe", "North America", "Africa", "Oceania", "Antarctica", "South America"
    };
}
