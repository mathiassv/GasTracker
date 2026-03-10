using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using GasTracker.Data;
using GasTracker.Data.Interfaces;
using GasTracker.Web.Components;
using GasTracker.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite
builder.Services.AddDbContext<GasTrackerDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository / Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// App services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<FuelCalculatorService>();
builder.Services.AddSingleton<UnitConversionService>();
builder.Services.AddSingleton<DisplayFormatter>();

// Authentication: Cookies + Google OAuth
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(o =>
{
    o.LoginPath = "/login";
    o.ExpireTimeSpan = TimeSpan.FromDays(30);
    o.SlidingExpiration = true;
})
.AddGoogle(o =>
{
    o.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    o.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    o.Scope.Add("email");
    o.Scope.Add("profile");
    o.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
    o.Events.OnTicketReceived = async ctx =>
    {
        // JIT user provisioning on every login
        var uow = ctx.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var sub = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var email = ctx.Principal?.FindFirstValue(ClaimTypes.Email) ?? "";
        var name = ctx.Principal?.FindFirstValue(ClaimTypes.Name) ?? email;

        if (!string.IsNullOrEmpty(sub))
        {
            var userService = ctx.HttpContext.RequestServices.GetRequiredService<UserService>();
            var user = await userService.GetOrCreateAsync(sub, email, name);

            // Add internal userId claim to principal so components can read it
            var identity = (ClaimsIdentity)ctx.Principal!.Identity!;
            identity.AddClaim(new Claim("app_user_id", user.Id.ToString()));
        }
    };
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// Blazorise
builder.Services
    .AddBlazorise(options => options.Immediate = true)
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GasTrackerDbContext>();
    Directory.CreateDirectory(Path.GetDirectoryName(db.Database.GetDbConnection().DataSource) ?? "data");
    await db.Database.MigrateAsync();
}

// Trust X-Forwarded-For / X-Forwarded-Proto from Apache reverse proxy
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
  ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Auth endpoints — must be real HTTP endpoints, not Blazor components
app.MapGet("/auth/login", async (HttpContext ctx) =>
    await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" }));

app.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Redirect("/login");
});

// Dev-only login — bypasses Google OAuth with a hardcoded user
if (app.Environment.IsDevelopment())
{
    app.MapGet("/auth/dev-login", async (HttpContext ctx) =>
    {
        const string devSub = "dev-user-local";
        const string devEmail = "dev@localhost";
        const string devName = "Dev User";

        var userService = ctx.RequestServices.GetRequiredService<UserService>();
        var user = await userService.GetOrCreateAsync(devSub, devEmail, devName);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, devSub),
            new(ClaimTypes.Email, devEmail),
            new(ClaimTypes.Name, devName),
            new("app_user_id", user.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        ctx.Response.Redirect("/");
    });
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
