#define DEFAULT
#if NEVER
#elif DEFAULT
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ECommerce.Models; // Remplacez par le namespace réel de votre DbContext
using ECommerce.Authorization;
using System;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Charger explicitement la configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Optionnel : Affichage de toutes les clés pour débogage
/*
foreach (var kvp in builder.Configuration.AsEnumerable())
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}
*/

// Configuration de Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
});

// Configuration de Kestrel pour la production
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
    serverOptions.ListenAnyIP(443, listenOptions =>
    {
        // Remplacez ces valeurs par vos informations réelles de certificat
        listenOptions.UseHttps("certificat.pfx", "votre_mot_de_passe");
    });
});

// Connexion à la base de données
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<EcommerceDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configuration d'Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<EcommerceDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddRazorPages();

// Options de sécurité d'Identity
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

    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;
});

// Configuration du cookie d'authentification
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = false;
    options.Cookie.Name = "MyCookie";
    // Vous pouvez définir CookiePath ici si nécessaire
});

// Enregistrement des services personnalisés
builder.Services.AddSingleton<PayPalService>();
builder.Services.AddSingleton<RazorpayService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ZohoTokenService>();
builder.Services.AddScoped<ZohoEmailService>();

// Ajout des handlers d'autorisation
builder.Services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ContactManagerAuthorizationHandler>();

// Redirection HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443;
});

// Configuration du cache et de la session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// === CONFIGURATION DE L'AUTHENTIFICATION OPENID CONNECT AVEC AZURE AD ===

// Récupérer la section "Authentication:AzureAd" depuis la configuration
var azureAdSection = builder.Configuration.GetSection("Authentication:AzureAd");

// Afficher en debug le ClientId pour vérifier qu'il est chargé correctement
Console.WriteLine("DEBUG: AzureAd ClientId = " + azureAdSection["ClientId"]);

// Configurer l'authentification avec Cookie et OpenIdConnect
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie("Cookies", options =>
{
    // Exemple de configuration supplémentaire pour le cookie si besoin
    options.LoginPath = "/Identity/Account/Login";
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    // Affecter explicitement les valeurs depuis "Authentication:AzureAd"
    options.ClientId = azureAdSection["ClientId"];
    options.ClientSecret = azureAdSection["ClientSecret"];
    options.Authority = azureAdSection["Authority"];
    options.MetadataAddress = $"{azureAdSection["Authority"]}/.well-known/openid-configuration";
    
    options.GetClaimsFromUserInfoEndpoint = true;
    // La propriété "AuthenticationScheme" n'existe pas dans OpenIdConnectOptions, donc ne pas l'affecter.
    options.SignInScheme = "Cookies";
    // Utiliser "id_token" en tant que type de réponse.
    options.ResponseType = "id_token";
    options.SaveTokens = true;

    // Paramètres de validation du token
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = ClaimsIdentity.DefaultNameClaimType,
        RoleClaimType = ClaimsIdentity.DefaultRoleClaimType,
        AuthenticationType = CookieAuthenticationDefaults.AuthenticationScheme,
        ValidateIssuer = false
    };

    // Scopes nécessaires à l'application
    options.Scope.Clear(); // Pour repartir d'une configuration propre
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("roles");

    // Définir le CallbackPath (assurez-vous qu'il correspond à la redirection configurée dans Azure AD)
    options.CallbackPath = "/.auth/login/aad/callback";
});

var app = builder.Build();

// Migration et Seed de la base de données
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
        Serilog.Log.Error(ex, "Erreur pendant l'initialisation de la base de données");
    }
}

// Configuration des middlewares
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

try
{
    Serilog.Log.Information("Démarrage de l'application...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Serilog.Log.Fatal(ex, "Erreur critique : l'application ne peut pas démarrer.");
}
finally
{
    Serilog.Log.CloseAndFlush();
}
#endif
