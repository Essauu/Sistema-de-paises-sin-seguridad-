using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PaisApp.Services;

namespace PaisApp.Middleware;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _siguiente;

    public SessionValidationMiddleware(RequestDelegate siguiente)
    {
        _siguiente = siguiente;
    }

    public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionToken = context.User.FindFirst("SessionToken")?.Value;

            if (int.TryParse(userIdClaim, out var userId) && !string.IsNullOrEmpty(sessionToken))
            {
                var isValid = await sessionService.ValidateSessionAsync(userId, sessionToken);

                if (!isValid)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }
        }

        await _siguiente(context);
    }
}
