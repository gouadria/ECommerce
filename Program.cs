using Azure.Core;
using Azure.Identity;
using ECommerce.Authorization;
using ECommerce.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ─── § CONFIGURATION ──────────────────────────────────────────
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // ─── § SERILOG ─────────────────────────────────────────────────
        builder.Host.UseSerilog((ctx, svc, cfg) =>
            cfg.ReadFrom.Configuration(ctx.Configuration)
               .Enrich.FromLogContext()
               .Enrich.WithMachineName()
               .WriteTo.Console()
               .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
        );

        // ─── § DBCONTEXT AVEC MANAGED IDENTITY ─────────────────────────
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
                var trc   = new TokenRequestContext(new[]{ "https://database.windows.net/.default" });
                var token = credential.GetToken(trc, default);
                sqlConn.AccessToken = token.Token;
                Log.Information("Jeton Azure SQL récupéré");
            }
            catch
            {
                Log.Warning("Jeton Azure SQL non récupéré, connexion classique");
            }
            opts.UseSqlServer(sqlConn);
        });
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // ─── § IDENTITY ───────────────────────────────────────────────
        builder.Services.AddIdentity<IdentityUser, IdentityRole>(opts =>
        {
            opts.SignIn.RequireConfirmedAccount = true;
        })
        .AddEntityFrameworkStores<EcommerceDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.Configure<IdentityOptions>(opts =>
        {
            // Password
            opts.Password.RequireDigit           = true;
            opts.Password.RequireLowercase       = true;
            opts.Password.RequireUppercase       = true;
            opts.Password.RequireNonAlphanumeric = true;
            opts.Password.RequiredLength         = 6;
            opts.Password.RequiredUniqueChars    = 1;
            // Lockout
            opts.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
            opts.Lockout.MaxFailedAccessAttempts = 5;
            opts.Lockout.AllowedForNewUsers      = true;
            // User
            opts.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            opts.User.RequireUniqueEmail = false;
        });

        // ─── § AUTHENTICATION & AUTHORIZATION ─────────────────────────
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opts =>
            {
                // Chemins personnalisés
                opts.LoginPath        = "/Identity/Account/Login";
                opts.LogoutPath       = "/Identity/Account/Logout";
                opts.AccessDeniedPath = "/Admin/AccessDenied";

                // Override des redirects
                opts.Events = new CookieAuthenticationEvents
                {
                    // Non authentifié → page de login
                    OnRedirectToLogin = ctx =>
                    {
                        ctx.Response.Redirect(
                          opts.LoginPath +
                          "?returnUrl=" +
                          Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString)
                        );
                        return Task.CompletedTask;
                    },
                    // Authentifié mais pas autorisé → AccessDenied
                    OnRedirectToAccessDenied = ctx =>
                    {
                        ctx.Response.Redirect(opts.AccessDeniedPath);
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        // ─── § AUTRES SERVICES ────────────────────────────────────────
        builder.Services.AddRazorPages();
        builder.Services.AddHttpClient();
        builder.Services.AddSession(opts =>
        {
            opts.IdleTimeout    = TimeSpan.FromMinutes(30);
            opts.Cookie.HttpOnly   = true;
            opts.Cookie.IsEssential = true;
        });
        builder.Services.AddSingleton<PayPalService>();
        builder.Services.AddSingleton<RazorpayService>();
        builder.Services.AddScoped<ZohoTokenService>();
        builder.Services.AddScoped<ZohoEmailService>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactManagerAuthorizationHandler>();

        // ─── § BUILD & MIGRATE ───────────────────────────────────────
        var app = builder.Build();
        using(var scope = app.Services.CreateScope())
        {
            var svc = scope.ServiceProvider;
            try
            {
                var ctx = svc.GetRequiredService<EcommerceDbContext>();
                ctx.Database.Migrate();
                var pw  = builder.Configuration["SeedUserPW"];
                await SeedData.Initialize(svc, pw);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur migration/seed");
            }
        }

        // ─── § MIDDLEWARE PIPELINE ────────────────────────────────────
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

