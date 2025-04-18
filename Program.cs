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

        // Serilog
        builder.Host.UseSerilog((context, services, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration)
                  .Enrich.FromLogContext()
                  .Enrich.WithMachineName()
                  .WriteTo.Console()
                  .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
        });

        // Database connection with Azure Managed Identity or SQL auth
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        var managedIdClientId = builder.Configuration["ManagedIdentity:ClientId"];
        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(managedIdClientId))
            credentialOptions.ManagedIdentityClientId = managedIdClientId;
        var credential = new DefaultAzureCredential(credentialOptions);

        builder.Services.AddDbContext<EcommerceDbContext>(options =>
        {
            var sqlConn = new SqlConnection(connectionString);
            try
            {
                var tokenRequest = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
                sqlConn.AccessToken = credential.GetToken(tokenRequest, default).Token;
                Log.Information("Azure SQL AccessToken applied");
            }
            catch
            {
                Log.Warning("Managed Identity token failed, using connection string credentials");
            }
            options.UseSqlServer(sqlConn);
        });
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Identity setup
        builder.Services.AddIdentity<IdentityUser, IdentityRole>(opts =>
        {
            opts.SignIn.RequireConfirmedAccount = true;
        })
        .AddEntityFrameworkStores<EcommerceDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.Configure<IdentityOptions>(opts =>
        {
            opts.Password.RequireDigit = true;
            opts.Password.RequireLowercase = true;
            opts.Password.RequireUppercase = true;
            opts.Password.RequireNonAlphanumeric = true;
            opts.Password.RequiredLength = 6;
            opts.Password.RequiredUniqueChars = 1;

            opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            opts.Lockout.MaxFailedAccessAttempts = 5;
            opts.Lockout.AllowedForNewUsers = true;

            opts.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            opts.User.RequireUniqueEmail = false;
        });

        // Authentication & Authorization
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                // Cookie settings
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Admin/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(12);
                options.SlidingExpiration = false;
                options.Cookie.Name = "MyCookie";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                // Override default 403 handling
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToAccessDenied = ctx =>
                    {
                        ctx.Response.Redirect(options.AccessDeniedPath + "?ReturnUrl=" + Uri.EscapeDataString(ctx.Request.Path));
                        return Task.CompletedTask;
                    }
                };
            })
            .AddOpenIdConnect("AzureAD", options =>
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
                    OnRemoteFailure = ctx =>
                    {
                        // Azure AD denied (e.g. no consent)
                        ctx.HandleResponse();
                        ctx.Response.Redirect("/Admin/AccessDenied");
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();

        // Razor, Sessions, HTTP
        builder.Services.AddRazorPages();
        builder.Services.AddSession(opts =>
        {
            opts.IdleTimeout = TimeSpan.FromMinutes(30);
            opts.Cookie.HttpOnly = true;
            opts.Cookie.IsEssential = true;
        });

        // App-specific services
        builder.Services.AddSingleton<PayPalService>();
        builder.Services.AddSingleton<RazorpayService>();
        builder.Services.AddScoped<ZohoTokenService>();
        builder.Services.AddScoped<ZohoEmailService>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ContactManagerAuthorizationHandler>();

        var app = builder.Build();

        // Database migrations & seed
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
            catch(Exception ex)
            {
                Log.Error(ex, "Database init error");
            }
        }

        // Pipeline
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
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        await app.RunAsync();
    }
}

