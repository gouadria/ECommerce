using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ECommerce.Models; // Remplace par ton vrai namespace
using ECommerce.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Azure.Identity;
using Azure.Core;
using Microsoft.Data.SqlClient;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        string clientId = builder.Configuration["Authentication:AzureAd:ClientId"];
        if (string.IsNullOrEmpty(clientId))
        {
            throw new Exception("AzureAd ClientId introuvable dans la configuration !");
        }

        // Serilog config
        builder.Host.UseSerilog((context, services, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration)
                  .Enrich.FromLogContext()
                  .Enrich.WithMachineName()
                  .WriteTo.Console()
                  .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        var managedIdentitySection = builder.Configuration.GetSection("ManagedIdentity");
        var managedIdentityClientId = managedIdentitySection["ClientId"];

        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
        }

        var credential = new DefaultAzureCredential(credentialOptions);

        builder.Services.AddDbContext<EcommerceDbContext>(options =>
        {
            SqlConnection connection;
            try
            {
                var token = credential.GetToken(new TokenRequestContext(new[] { "https://database.windows.net/" }));
                connection = new SqlConnection(connectionString)
                {
                    AccessToken = token.Token
                };
                Serilog.Log.Information("Jeton Managed Identity utilisé.");
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Erreur lors de l'utilisation de la Managed Identity, fallback sur la chaîne classique.");
                connection = new SqlConnection(connectionString);
            }

            options.UseSqlServer(connection);
        });

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
        })
        .AddEntityFrameworkStores<EcommerceDbContext>()
        .AddDefaultTokenProviders();

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

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = false;
            options.Cookie.Name = "MyCookie";
        });

        builder.Services.AddRazorPages();
        builder.Services.AddHttpClient();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // Services personnalisés
        builder.Services.AddSingleton<PayPalService>();
        builder.Services.AddSingleton<RazorpayService>();
        builder.Services.AddScoped<ZohoTokenService>();
        builder.Services.AddScoped<ZohoEmailService>();

        // Authorization Handlers
        builder.Services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactManagerAuthorizationHandler>();

        // Auth Azure AD
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

            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.GetClaimsFromUserInfoEndpoint = true;
            options.SignInScheme = "Cookies";
            options.ResponseType = "code";
            options.SaveTokens = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = ClaimsIdentity.DefaultNameClaimType,
                RoleClaimType = ClaimsIdentity.DefaultRoleClaimType,
                ValidateIssuer = false
            };

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("roles");

            options.CallbackPath = "/.auth/login/aad/callback";
        });

        builder.Services.AddHttpsRedirection(options =>
        {
            options.HttpsPort = 443;
        });

        builder.Services.AddDistributedMemoryCache();

        var app = builder.Build();

        // Migration automatique + seed
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

        // Middleware
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
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSession();

        app.MapRazorPages();

        await app.RunAsync();
    }
}

