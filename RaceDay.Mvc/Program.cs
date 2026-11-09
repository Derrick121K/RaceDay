using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

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
