using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
// using Serilog.Enrichers.Thread;  // Cette directive est supprimée pour éviter l'erreur
using ECommerce.Models;              // Remplacez par le namespace réel de votre DbContext
using ECommerce.Authorization;       // Assurez-vous que ce namespace est correct et que vos handlers implémentent IAuthorizationHandler
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Charge explicitement le fichier de configuration
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Configuration de Serilog
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                // .Enrich.WithThreadId() 
                // Si vous souhaitez inclure l'ID du thread, assurez-vous que le package Serilog.Enrichers.Thread est installé
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
            // La propriété AuthenticationScheme n'est pas nécessaire ici, car AddAuthentication définit déjà le schéma.
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = false;
            options.Cookie.Name = "MyCookie";
        });

        // Enregistrement des services personnalisés
        builder.Services.AddSingleton<PayPalService>();
        builder.Services.AddSingleton<RazorpayService>();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<ZohoTokenService>();
        builder.Services.AddScoped<ZohoEmailService>();

        // Ajout des handlers d'autorisation
        builder.Services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactManagerAuthorizationHandler>();

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

        // Configuration de l'authentification OpenID Connect avec Azure AD
        var azureAdSection = builder.Configuration.GetSection("Authentication:AzureAd");
        var authLogger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("AzureAd");
        authLogger.LogInformation("AzureAd ClientId: {ClientId}", azureAdSection["ClientId"]);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie("Cookies", options =>
        {
            options.LoginPath = "/Identity/Account/Login";
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = azureAdSection["ClientId"];
            options.ClientSecret = azureAdSection["ClientSecret"];
            options.Authority = azureAdSection["Authority"];
            options.MetadataAddress = $"{azureAdSection["Authority"]}/.well-known/openid-configuration";

            // Forcer HTTPS : si en développement, vous pouvez le désactiver pour tester, sinon, il doit être en HTTPS.
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
            else
            {
                options.RequireHttpsMetadata = true;
            }

            options.GetClaimsFromUserInfoEndpoint = true;
            options.SignInScheme = "Cookies";
            options.ResponseType = "code";  // Utiliser "code" pour Authorization Code Flow
            options.SaveTokens = true;

            // Paramètres de validation du token
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = ClaimsIdentity.DefaultNameClaimType,
                RoleClaimType = ClaimsIdentity.DefaultRoleClaimType,
                ValidateIssuer = false
            };

            // Scopes nécessaires à l'application
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("roles");

            // Définir le CallbackPath (doit être enregistré dans Azure AD)
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
    }
}


