using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace The_Watch_Vault.Components;

/// <summary>
/// Flows cookie authentication into interactive Blazor Server circuits and revalidates periodically.
/// Required for cookie auth: without a <see cref="RevalidatingServerAuthenticationStateProvider"/>,
/// <see cref="AuthenticationStateProvider.GetAuthenticationStateAsync"/> may not reflect the signed-in user.
/// </summary>
internal sealed class CookieRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IHttpContextAccessor httpContextAccessor)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (authenticationState.User.FindFirstValue(ClaimTypes.NameIdentifier) is null or "")
        {
            return false;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            // Interactive callbacks often run without an HTTP request; keep the circuit principal.
            return true;
        }

        var result = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return result.Succeeded && result.Principal?.Identity?.IsAuthenticated == true;
    }
}
