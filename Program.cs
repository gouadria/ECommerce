using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ECommerce.Models;
using ECommerce.Authorization;
using Microsoft.AspNetCore.Authorization;

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

        // DbContext avec connexion SQL standard
        builder.Services.AddDbContext<EcommerceDbContext>(options =>
            options.UseSqlServer(connectionString));

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

        // Authentification par cookie seulement (pas d’Azure AD / OpenId)
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();

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
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();

        await app.RunAsync();
    }
}
