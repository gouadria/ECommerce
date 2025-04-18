using Azure.Core;
using Azure.Identity;
using ECommerce.Authorization;
using ECommerce.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using Serilog;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ─── Configuration ───────────────────────────────────────────
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Vérification du clientId Azure AD
        string clientId = builder.Configuration["Authentication:AzureAd:ClientId"];
        if (string.IsNullOrEmpty(clientId))
            throw new Exception("AzureAd ClientId introuvable dans la configuration !");

        // ─── Serilog ──────────────────────────────────────────────────
        builder.Host.UseSerilog((ctx, svc, cfg) =>
            cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
        );

        // ─── DbContext avec Token Azure (Managed Identity) ─────────────
        var connString = builder.Configuration.GetConnectionString("DefaultConnection");
        var miClientId = builder.Configuration["ManagedIdentity:ClientId"];
        var credOpts = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(miClientId))
            credOpts.ManagedIdentityClientId = miClientId;
        var credential = new DefaultAzureCredential(credOpts);

        builder.Services.AddDbContext<EcommerceDbContext>(opts =>
        {
            var sqlConn = new SqlConnection(connString);
            try
            {
                var trc = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
                var token = credential.GetToken(trc, default);
                sqlConn.AccessToken = token.Token;
                Log.Information("Jeton d'accès Azure récupéré.");
            }
            catch
            {
                Log.Warning("Impossible de récupérer le token, connexion sans AccessToken.");
            }
            opts.UseSqlServer(sqlConn);
        });

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // ─── Identity ─────────────────────────────────────────────────
        builder.Services.AddIdentity<IdentityUser, IdentityRole>(opts =>
        {
            opts.SignIn.RequireConfirmedAccount = true;
        })
        .AddEntityFrameworkStores<EcommerceDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.Configure<IdentityOptions>(opts =>
        {
            // -- Password
            opts.Password.RequireDigit = true;
            opts.Password.RequireLowercase = true;
            opts.Password.RequireUppercase = true;
            opts.Password.RequireNonAlphanumeric = true;
            opts.Password.RequiredLength = 6;
            opts.Password.RequiredUniqueChars = 1;
            // -- Lockout
            opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            opts.Lockout.MaxFailedAccessAttempts = 5;
            opts.Lockout.AllowedForNewUsers = true;
            // -- User
            opts.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            opts.User.RequireUniqueEmail = false;
        });

        // ─── Cookie & OpenID Connect ──────────────────────────────────
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme  = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(opts =>
        {
            opts.LoginPath        = "/Identity/Account/Login";
            opts.LogoutPath       = "/Identity/Account/Logout";
            opts.AccessDeniedPath = "/Admin/AccessDenied";

            opts.Events = new CookieAuthenticationEvents
            {
                OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.Redirect("/Admin/AccessDenied");
                    return Task.CompletedTask;
                }
            };
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, opts =>
        {
            builder.Configuration.Bind("Authentication:AzureAd", opts);
            opts.ResponseType = "code";
            opts.SaveTokens   = true;
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = "roles"
            };
            opts.Events = new OpenIdConnectEvents
            {
                OnRemoteFailure = ctx =>
                {
                    // En cas de refus de consentement ou autre échec OIDC
                    ctx.HandleResponse();
                    ctx.Response.Redirect("/Admin/AccessDenied");
                    return Task.CompletedTask;
                }
            };
        });

        builder.Services.AddAuthorization();

        // ─── Autres services ─────────────────────────────────────────
        builder.Services.AddRazorPages();
        builder.Services.AddHttpClient();
        builder.Services.AddSession(opts =>
        {
            opts.IdleTimeout = TimeSpan.FromMinutes(30);
            opts.Cookie.HttpOnly = true;
            opts.Cookie.IsEssential = true;
        });
        builder.Services.AddSingleton<PayPalService>();
        builder.Services.AddSingleton<RazorpayService>();
        builder.Services.AddScoped<ZohoTokenService>();
        builder.Services.AddScoped<ZohoEmailService>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactManagerAuthorizationHandler>();

        // ─── Build & migration ───────────────────────────────────────
        var app = builder.Build();
        using(var scope = app.Services.CreateScope())
        {
            var svc = scope.ServiceProvider;
            try
            {
                var ctx = svc.GetRequiredService<EcommerceDbContext>();
                ctx.Database.Migrate();
                var pw = builder.Configuration.GetValue<string>("SeedUserPW");
                await SeedData.Initialize(svc, pw);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur pendant la migration/seed.");
            }
        }

        // ─── Pipeline ────────────────────────────────────────────────
        if (app.Environment.IsDevelopment())
            app.UseDeveloperExceptionPage();
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
