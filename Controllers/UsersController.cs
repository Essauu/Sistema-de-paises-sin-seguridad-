using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisApp.Data;
using PaisApp.Models;
using System.Text;

namespace PaisApp.Controllers;

[Authorize(Policy = "Admin")]
public class UsersController : Controller
{
    private readonly PaisesContext _contexto;
    private readonly ILogger<UsersController> _registrador;

    public UsersController(PaisesContext context, ILogger<UsersController> logger)
    {
        _contexto = context;
        _registrador = logger;
    }

    public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 20)
    {
        var query = _contexto.Cgusuarios.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u =>
                u.Username.Contains(search) ||
                u.Nombre.Contains(search) ||
                u.Apellido1.Contains(search) ||
                u.Apellido2.Contains(search));
        }

        var totalElementos = await query.CountAsync();
        var usuarios = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalElementos / pageSize);
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalElementos;

        return View(usuarios);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Cgusuarios usuario)
    {
        if (string.IsNullOrEmpty(usuario.ContrasenaPlana))
        {
            ModelState.AddModelError("ContrasenaPlana", "La contraseña es requerida.");
            return View(usuario);
        }

        if (usuario.ContrasenaPlana != usuario.ConfirmarContrasena)
        {
            ModelState.AddModelError("ConfirmarContrasena", "Las contraseñas no coinciden.");
            return View(usuario);
        }

        if (ModelState.IsValid)
        {
            if (await _contexto.Cgusuarios.AnyAsync(u => u.Username == usuario.Username))
            {
                ModelState.AddModelError("Username", "El nombre de usuario ya existe.");
                return View(usuario);
            }

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.ContrasenaPlana);
            usuario.Status = 1;

            _contexto.Add(usuario);
            await _contexto.SaveChangesAsync();

            _registrador.LogInformation("Admin creó usuario {Username} con nivel {Nivel}", usuario.Username, usuario.Nivel);
            TempData["Success"] = $"Usuario {usuario.Username} creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        return View(usuario);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var usuario = await _contexto.Cgusuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Cgusuarios usuario)
    {
        if (id != usuario.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var existente = await _contexto.Cgusuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (existente == null) return NotFound();

                usuario.Password = existente.Password;
                usuario.Status = existente.Status;

                if (!string.IsNullOrEmpty(usuario.ContrasenaPlana))
                {
                    if (usuario.ContrasenaPlana != usuario.ConfirmarContrasena)
                    {
                        ModelState.AddModelError("ConfirmarContrasena", "Las contraseñas no coinciden.");
                        return View(usuario);
                    }
                    usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.ContrasenaPlana);
                }

                _contexto.Update(usuario);
                await _contexto.SaveChangesAsync();

                _registrador.LogInformation("Admin actualizó usuario {Username}", usuario.Username);
                TempData["Success"] = $"Usuario {usuario.Username} actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _contexto.Cgusuarios.AnyAsync(u => u.Id == id))
                    return NotFound();
                throw;
            }
        }

        return View(usuario);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var usuario = await _contexto.Cgusuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        return View(usuario);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var usuario = await _contexto.Cgusuarios.FindAsync(id);
        if (usuario != null)
        {
            _contexto.Cgusuarios.Remove(usuario);
            await _contexto.SaveChangesAsync();
            _registrador.LogInformation("Admin eliminó usuario {Username}", usuario.Username);
            TempData["Success"] = $"Usuario {usuario.Username} eliminado.";
        }

        return RedirectToAction(nameof(Index));
    }
}
