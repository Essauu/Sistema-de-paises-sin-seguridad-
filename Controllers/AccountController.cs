using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisApp.Data;
using PaisApp.Models;
using PaisApp.Services;
using System.Security.Claims;
using System.Text;

namespace PaisApp.Controllers;

public class AccountController : Controller
{
    private readonly PaisesContext _contexto;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AccountController> _registrador;

    public AccountController(PaisesContext context, ISessionService sessionService, ILogger<AccountController> logger)
    {
        _contexto = context;
        _sessionService = sessionService;
        _registrador = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? urlRetorno = null)
    {
        ViewData["ReturnUrl"] = urlRetorno;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? urlRetorno = null)
    {
        ViewData["ReturnUrl"] = urlRetorno;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Usuario y contraseña son requeridos.");
            return View();
        }

        var usuario = await _contexto.Cgusuarios
            .FirstOrDefaultAsync(u => u.Username == username);

        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, "Usuario no encontrado.");
            return View();
        }

        if (usuario.Status != 1)
        {
            ModelState.AddModelError(string.Empty, "La cuenta está desactivada.");
            return View();
        }

        var hashAlmacenado = usuario.Password;
        var contrasenaValida = BCrypt.Net.BCrypt.Verify(password, hashAlmacenado);

        if (!contrasenaValida)
        {
            ModelState.AddModelError(string.Empty, "Contraseña incorrecta.");
            return View();
        }

        var tokenSesion = Guid.NewGuid().ToString("N");
        var puedeIniciar = await _sessionService.CanStartSessionAsync(usuario.Id);
        if (!puedeIniciar)
        {
            ModelState.AddModelError(string.Empty, "Ya tiene una sesión activa. Cierre la sesión anterior primero.");
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim("FullName", $"{usuario.Nombre} {usuario.Apellido1} {usuario.Apellido2}".Trim()),
            new Claim("Nivel", usuario.Nivel.ToString()),
            new Claim("SessionToken", tokenSesion),
        };

        var identidadClaims = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var propiedadesAutenticacion = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidadClaims),
            propiedadesAutenticacion);

        await _sessionService.RegisterSessionAsync(usuario.Id, tokenSesion);

        _registrador.LogInformation("Usuario {Username} inició sesión", username);

        return LocalRedirect(urlRetorno ?? "/");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tokenSesion = User.FindFirst("SessionToken")?.Value;
        if (int.TryParse(userId, out var id) && !string.IsNullOrEmpty(tokenSesion))
        {
            await _sessionService.CloseSessionAsync(id, tokenSesion);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(Cgusuarios usuario)
    {
        if (string.IsNullOrEmpty(usuario.ContrasenaPlana))
        {
            ModelState.AddModelError("PasswordPlain", "La contraseña es requerida.");
            return View(usuario);
        }

        if (usuario.ContrasenaPlana != usuario.ConfirmarContrasena)
        {
            ModelState.AddModelError("ConfirmPassword", "Las contraseñas no coinciden.");
            return View(usuario);
        }

        if (ModelState.IsValid)
        {
            if (await _contexto.Cgusuarios.AnyAsync(u => u.Username == usuario.Username))
            {
                ModelState.AddModelError("Username", "El nombre de usuario ya existe.");
                return View(usuario);
            }

            usuario.Nivel = 1;
            usuario.Status = 1;
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.ContrasenaPlana);

            _contexto.Add(usuario);
            await _contexto.SaveChangesAsync();

            var tokenSesion = Guid.NewGuid().ToString("N");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Username),
                new Claim("FullName", $"{usuario.Nombre} {usuario.Apellido1} {usuario.Apellido2}".Trim()),
                new Claim("Nivel", usuario.Nivel.ToString()),
                new Claim("SessionToken", tokenSesion),
            };

            var identidadClaims = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var propiedadesAutenticacion = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidadClaims),
                propiedadesAutenticacion);

            await _sessionService.RegisterSessionAsync(usuario.Id, tokenSesion);

            _registrador.LogInformation("Usuario {Username} se registró como nivel {Nivel}", usuario.Username, usuario.Nivel);
            return RedirectToAction("Index", "Home");
        }

        return View(usuario);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
