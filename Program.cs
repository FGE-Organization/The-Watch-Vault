using The_Watch_Vault.Components;
using The_Watch_Vault.Data;
using The_Watch_Vault.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register the user repository (using in-memory storage for now)
// This can be easily swapped to a database implementation later
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

// Register Firestore service for watches and brands
builder.Services.AddSingleton<FirestoreService>();
builder.Services.AddTransient<FirestoreSeeder>(sp => new FirestoreSeeder(
    sp.GetRequiredService<FirestoreService>()._db,
    sp.GetRequiredService<IWebHostEnvironment>(),
    sp.GetRequiredService<ILogger<FirestoreSeeder>>()
));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
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
});

builder.Services.AddAuthorization();

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

// Must be registered before MapRazorComponents to avoid route ambiguity with /{*path}
app.MapGet("/login-google", async (HttpContext context) =>
{
    var properties = new AuthenticationProperties { RedirectUri = "/" };
    await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
});
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync();
    context.Response.Redirect("/");
});
app.MapPost("/login-submit", async (HttpContext context, IConfiguration config) =>
{
    var email         = context.Request.Form["email"].ToString();
    var password      = context.Request.Form["password"].ToString();
    var adminEmail    = config["Admin:Email"] ?? "";
    var adminPassword = config["Admin:Password"] ?? "";

    if (string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase)
        && password == adminPassword)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, email),
            new(System.Security.Claims.ClaimTypes.Role, "Admin")
        };
        var identity  = new System.Security.Claims.ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        context.Response.Redirect("/admin");
    }
    else
    {
        context.Response.Redirect("/login?error=invalid");
    }
}).DisableAntiforgery();

app.MapPost("/admin/toggle-instock", async (HttpContext context, FirestoreService firestoreService) =>
{
    if (!context.User.Identity?.IsAuthenticated == true || !context.User.IsInRole("Admin"))
    { context.Response.Redirect("/login"); return; }
    var id = context.Request.Form["id"].ToString();
    var current = context.Request.Form["current"].ToString() == "true";
    await firestoreService.UpdateInStockAsync(id, !current);
    context.Response.Redirect("/admin?status=updated");
}).DisableAntiforgery();

app.MapPost("/admin/delete-watch", async (HttpContext context, FirestoreService firestoreService) =>
{
    if (!context.User.Identity?.IsAuthenticated == true || !context.User.IsInRole("Admin"))
    { context.Response.Redirect("/login"); return; }
    var id = context.Request.Form["id"].ToString();
    await firestoreService.DeleteWatchAsync(id);
    context.Response.Redirect("/admin?status=deleted");
}).DisableAntiforgery();

app.MapPost("/admin/add-watch", async (HttpContext context, FirestoreService firestoreService) =>
{
    if (!context.User.Identity?.IsAuthenticated == true || !context.User.IsInRole("Admin"))
    { context.Response.Redirect("/login"); return; }
    var f = context.Request.Form;
    decimal.TryParse(f["price"].ToString(), out var price);
    var inStock = f["inStock"].ToString() == "on";
    await firestoreService.AddWatchAsync(
        f["brand"].ToString(), f["name"].ToString(), f["model"].ToString(),
        f["movement"].ToString(), f["description"].ToString(), f["imageUrl"].ToString(),
        price, inStock);
    context.Response.Redirect("/admin?status=added");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapStaticAssets();

// Seed Firestore with watches.json data if not already populated
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<FirestoreSeeder>();
    await seeder.SeedAsync();
}

app.Run();
