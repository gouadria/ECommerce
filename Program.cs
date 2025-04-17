 using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ECommerce.Models;
using ECommerce.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

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

        // Configuration du DbContext
        builder.Services.AddDbContext<EcommerceDbContext>(options =>
        {
            var sqlConnection = new SqlConnection(connectionString);
            options.UseSqlServer(sqlConnection);
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

            // ✅ Correction : définition des bons chemins de redirection
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/Admin/AccessDenied";
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

        builder.Services.AddDistributedMemoryCache();

        var app = builder.Build();

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

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.Urls.Add("http://localhost:5000");
        app.Urls.Add("https://localhost:5001");

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
