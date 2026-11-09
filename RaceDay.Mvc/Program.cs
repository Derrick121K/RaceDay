using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

// Give MVC its own persistent Development key ring. DPAPI scopes the key
// encryption to the current Windows user; production key configuration is
// left to the deployment environment.
if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();

    var dataProtection = builder.Services.AddDataProtection()
        .SetApplicationName("RaceDay.Mvc");

    if (OperatingSystem.IsWindows())
    {
        var keyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RaceDay", "Development", "DataProtection", "Mvc");

        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
            .ProtectKeysWithDpapi(protectToLocalMachine: false);
    }
}

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiSessionCookieHandler>();
builder.Services.AddHttpClient<RaceDayApiClient>((services, client) =>
    {
        var baseUrl = services.GetRequiredService<IConfiguration>()["Api:BaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var apiUri) ||
            (apiUri.Scheme != Uri.UriSchemeHttps &&
             !builder.Environment.IsDevelopment()))
        {
            throw new InvalidOperationException(
                "Api:BaseUrl must be an absolute HTTPS URL outside Development.");
        }

        client.BaseAddress = apiUri;
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Browser session cookies are forwarded per request by the handler below.
        // A shared CookieContainer would mix sessions between MVC users.
        UseCookies = false,
        AllowAutoRedirect = false
    })
    .AddHttpMessageHandler<ApiSessionCookieHandler>();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".RaceDay.Mvc.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
