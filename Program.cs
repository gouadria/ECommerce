using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ECommerce.Models;
using ECommerce.Authorization;
using Microsoft.AspNetCore.Authorization;
using Azure.Identity;
using Azure.Core;
using Microsoft.Data.SqlClient;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuration
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        string clientId = builder.Configuration["Authentication:AzureAd:ClientId"];
        if (string.IsNullOrEmpty(clientId))
        {
            throw new Exception("AzureAd ClientId introuvable dans la configuration !");
        }

        // Serilog
        builder.Host.UseSerilog((context, services, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration)
                  .Enrich.FromLogContext()
                  .Enrich.WithMachineName()
                  .WriteTo.Console()
                  .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        var managedIdentityClientId = builder.Configuration["ManagedIdentity:ClientId"];

        // Authentification Azure
        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
        {
            credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
        }

        var credential = new DefaultAzureCredential(credentialOptions);

        builder.Services.AddDbContext<EcommerceDbContext>(options =>
        {
            var sqlConnection = new SqlConnection(connectionString);

            try
            {
                var tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
                var token = credential.GetToken(tokenRequestContext, default);
                sqlConnection.AccessToken = token.Token;
                Serilog.Log.Information("Jeton d'accès Azure utilisé pour la connexion SQL.");
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Échec de la récupération du jeton, utilisation de la connexion standard.");
            }

            options.UseSqlServer(sqlConnection);
        });

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Identity
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

        // Configuration des cookies
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = false;
            options.Cookie.Name = "MyCookie";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;

            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/Admin/AccessDenied";
        });

        // Authentification Azure AD
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            builder.Configuration.Bind("Authentication:AzureAd", options);
            options.ResponseType = "code";
            options.SaveTokens = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = "roles"
            };

            options.Events = new OpenIdConnectEvents
            {
                OnRemoteFailure = context =>
                {
                    if (context.Failure?.Message.Contains("access_denied") == true)
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/Admin/AccessDenied");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Autorisation
        builder.Services.AddAuthorization();

        // Razor Pages, Sessions, HTTP
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

        // Authorization handlers
        builder.Services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactManagerAuthorizationHandler>();

        var app = builder.Build();

        // Migration + seed
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

        // Pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
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
