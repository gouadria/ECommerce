#define DEFAULT
#if NEVER
#elif DEFAULT
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Serilog;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔒 Configuration du logging avec Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
});

// 🔐 Configuration de Kestrel pour production
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80); // HTTP (optionnel si derrière un reverse proxy)
    serverOptions.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps("certificat.pfx", "votre_mot_de_passe"); // À remplacer avec votre certificat
    });
});

// 🔧 Connexion à la base de données
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<EcommerceDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ⚙️ Configuration d'Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<EcommerceDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddRazorPages();

// 🔒 Options de sécurité Identity
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// 📦 Services personnalisés
builder.Services.AddSingleton<PayPalService>();
builder.Services.AddSingleton<RazorpayService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ZohoTokenService>();
builder.Services.AddScoped<ZohoEmailService>();

// 🔐 Authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ContactManagerAuthorizationHandler>();

// ➕ HTTPS Redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443;
});

// 💾 Cache et session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ⚡️ Configuration de l'authentification OpenID Connect avec Azure AD
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
})
.AddOpenIdConnect(options =>
{
    options.ClientId = builder.Configuration["Authentication:AzureAd:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:AzureAd:ClientSecret"];
    options.Authority = builder.Configuration["Authentication:AzureAd:Authority"];
    options.CallbackPath = builder.Configuration["Authentication:AzureAd:CallbackPath"] ?? "/signin-oidc"; // Utilisation de la valeur par défaut si non définie
    options.ResponseType = "code"; // Ajout recommandé
    options.SaveTokens = true;    // Ajout recommandé
});

var app = builder.Build();

// 🔁 Migration + Seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<EcommerceDbContext>();
        context.Database.Migrate();

        var testUserPw = builder.Configuration.GetValue<string>("SeedUserPW");
        await SeedData.Initialize(services, testUserPw);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Erreur pendant l'initialisation de la base de données");
    }
}

// 🔀 Middlewares
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// 🚀 Démarrage sécurisé
try
{
    Log.Information("Démarrage de l'application...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erreur critique : l'application ne peut pas démarrer.");
}
finally
{
    Log.CloseAndFlush();
}
#endif
