using System.Text.Json.Serialization;
using KopiYo.Common;
using KopiYo.Controllers.Api;
using KopiYo.Data;
using KopiYo.Models;
using KopiYo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

builder.Services.Configure<KopiYoSettings>(
    builder.Configuration.GetSection(KopiYoSettings.SectionName));

// PasswordHasher<T> tersedia dari shared framework Microsoft.AspNetCore.App —
// tidak perlu paket Identity apa pun. Stateless, jadi singleton.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddKopiYoServices();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "KopiYo.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromHours(10);   // kira-kira satu shift kasir
        options.SlidingExpiration = true;

        // KRITIS untuk layar POS: tanpa dua handler di bawah, cookie yang kedaluwarsa
        // membuat fetch() menerima HTTP 200 berisi HTML halaman login, lalu
        // JSON.parse meledak dengan "Unexpected token <" — error yang menyesatkan.
        // Permintaan ke /api harus dijawab status code, bukan redirect.
        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Layar POS mengirim antiforgery token lewat header (fetch tidak mengirim form field).
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

// [ApiController] secara default membalas error validasi dengan ValidationProblemDetails,
// bentuk yang berbeda dari ApiErrorDto milik kita. Diseragamkan di sini supaya
// pos.js cukup menangani satu format: { errors: [...] }.
builder.Services.Configure<ApiBehaviorOptions>(o =>
    o.InvalidModelStateResponseFactory = PosApiController.MapModelStateErrors);

builder.Services.AddControllersWithViews(options =>
    {
        // Default aman: SEMUA action butuh login, yang publik harus opt-out
        // pakai [AllowAnonymous]. Lebih sulit lupa daripada menempel [Authorize]
        // satu per satu di controller baru.
        //
        // PENTING: pakai MVC filter, JANGAN AuthorizationOptions.FallbackPolicy atau
        // .RequireAuthorization() di endpoint. Di .NET 9/10 MapStaticAssets()
        // mendaftarkan file statis sebagai endpoint, sehingga fallback policy akan
        // ikut me-redirect bootstrap.min.css ke halaman login -> halaman login tanpa CSS.
        options.Filters.Add(new AuthorizeFilter());

        // CSRF otomatis untuk semua request non-GET, termasuk POST ke /api,
        // karena autentikasinya berbasis cookie.
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddJsonOptions(o =>
        // Enum dikirim sebagai string ("Cash", bukan 1) supaya payload API kebaca manusia.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Urutan ini tidak bisa ditawar: Routing -> Authentication -> Authorization.
// Template bawaan tidak punya UseAuthentication(); kalau lupa menambahkannya,
// User.Identity.IsAuthenticated selalu false dan login berhasil pun tetap
// dilempar balik ke halaman login terus-menerus.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await DbInitializer.SeedAsync(
        sp.GetRequiredService<AppDbContext>(),
        sp.GetRequiredService<IPasswordHasher<User>>(),
        sp.GetRequiredService<ILogger<Program>>(),
        app.Environment.IsDevelopment());
}

app.Run();
