using The_Watch_Vault.Components;
using The_Watch_Vault.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, CookieRevalidatingAuthenticationStateProvider>();

builder.Services.AddScoped<CartUiState>();

// Configure Firestore
var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];
var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"];

if (!string.IsNullOrEmpty(firebaseProjectId) && !string.IsNullOrEmpty(firebaseCredentialsPath))
{
    var fullPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, firebaseCredentialsPath);
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", fullPath);
    
    var firestoreBuilder = new Google.Cloud.Firestore.FirestoreDbBuilder
    {
        ProjectId = firebaseProjectId,
        GrpcAdapter = Google.Api.Gax.Grpc.GrpcNetClientAdapter.Default
    };
    var firestoreDb = firestoreBuilder.Build();
    
    builder.Services.AddSingleton(firestoreDb);
    builder.Services.AddSingleton<IUserRepository, FirestoreUserRepository>();
    builder.Services.AddSingleton<IWatchRepository, FirestoreWatchRepository>();
    builder.Services.AddSingleton<ICartRepository, FirestoreCartRepository>();
    builder.Services.AddSingleton<IPurchaseRepository, FirestorePurchaseRepository>();
}
else
{
    // Fallback to in-memory if Firebase is not configured
    builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = ".TheWatchVault.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    // REQUIRED FOR OAUTH
    options.Cookie.SameSite = SameSiteMode.None;

    options.LoginPath = "/login";
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
        ?? throw new InvalidOperationException("Google ClientId missing");

    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
        ?? throw new InvalidOperationException("Google ClientSecret missing");

    options.CallbackPath = "/signin-google";

    // 🔥 CRITICAL FIX FOR "oauth state missing"
    options.CorrelationCookie.SameSite = SameSiteMode.None;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

    options.SaveTokens = true;
    
    options.Events.OnTicketReceived = async context =>
    {
        var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        
        if (!string.IsNullOrEmpty(email))
        {
            var existingUser = await userRepository.GetByEmailAsync(email);
            if (existingUser == null)
            {
                var newUser = new The_Watch_Vault.Models.User
                {
                    Email = email,
                    Name = name ?? "Google User",
                };
                var created = await userRepository.CreateAsync(newUser);
                
                var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (identity != null)
                {
                    var existingId = identity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (existingId != null) identity.RemoveClaim(existingId);
                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, created.Id!));
                }
            }
            else
            {
                var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (identity != null)
                {
                    var existingId = identity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (existingId != null) identity.RemoveClaim(existingId);
                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, existingUser.Id!));
                }
            }
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

builder.Services.AddScoped(sp =>
{
    var navMan = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navMan.BaseUri) };
});

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var watchRepo = scope.ServiceProvider.GetService<IWatchRepository>();
    if (watchRepo != null)
    {
        await watchRepo.CheckAndSeedAsync();
    }
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to seed watches during startup.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// Note: MapStaticAssets() below handles the remaining static assets in .NET 9.

app.UseRouting();

app.UseCookiePolicy();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// OAuth endpoints - these bypass the normal flow and work with anti-forgery
app.MapGet("/login-google", async (HttpContext context) =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = "/"
    };

    await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
});
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync();
    context.Response.Redirect("/");
});

app.MapPost("/auth/login", async ([Microsoft.AspNetCore.Mvc.FromForm] string email, [Microsoft.AspNetCore.Mvc.FromForm] string password, [Microsoft.AspNetCore.Mvc.FromForm] string? rememberMe, HttpContext context, IUserRepository userRepository) =>
{
    try
    {
        var user = await userRepository.GetByEmailAsync(email);
        if (user != null && !string.IsNullOrEmpty(user.PasswordHash) && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Name ?? user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id ?? string.Empty)
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(identity), new AuthenticationProperties
            {
                IsPersistent = rememberMe == "true"
            });
            return Results.Redirect("/shop");
        }
        return Results.Redirect("/login?error=Invalid email or password");
    }
    catch (Exception ex)
    {
        return Results.Redirect($"/login?error=An error occurred during login: {System.Net.WebUtility.UrlEncode(ex.Message)}");
    }
});


app.Run();
