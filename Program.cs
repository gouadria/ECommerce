// ... tous les using identiques sauf ceux liés à OpenIdConnect
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Azure.Identity;
using Azure.Core;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

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

// Récupère l'ID de l'identité managée si défini
var managedIdentityClientId = builder.Configuration["ManagedIdentity:ClientId"];
var credentialOptions = new DefaultAzureCredentialOptions();
if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
{
    credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
}

var credential = new DefaultAzureCredential(credentialOptions);

// Configuration DbContext avec AccessToken
builder.Services.AddDbContext<EcommerceDbContext>(options =>
{
    var sqlConnection = new SqlConnection(connectionString);
    try
    {
        var tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
        var token = credential.GetToken(tokenRequestContext, default);
        sqlConnection.AccessToken = token.Token;
        Serilog.Log.Information("AccessToken Azure récupéré avec succès.");
    }
    catch (Exception ex)
    {
        Serilog.Log.Warning(ex, "Échec du jeton, connexion sans AccessToken.");
    }

    options.UseSqlServer(sqlConnection);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<EcommerceDbContext>()
.AddDefaultTokenProviders();

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

// Services custom
builder.Services.AddSingleton<PayPalService>();
builder.Services.AddSingleton<RazorpayService>();
builder.Services.AddScoped<ZohoTokenService>();
builder.Services.AddScoped<ZohoEmailService>();

// Authorization handlers (facultatif si tu les utilises)
builder.Services.AddScoped<IAuthorizationHandler, ContactIsOwnerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ContactAdministratorsAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ContactManagerAuthorizationHandler>();

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443;
});

builder.Services.AddDistributedMemoryCache();

var app = builder.Build();

// Migration + Seed
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
        Serilog.Log.Error(ex, "Erreur migration/seed");
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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapRazorPages();

await app.RunAsync();
