using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;
using RaceDay.Api.Development;
using Microsoft.AspNetCore.DataProtection;



var builder = WebApplication.CreateBuilder(args);

// In Development, keep each app's persistent key ring under the current
// Windows user's local profile and protect its keys with that user's DPAPI.
// This avoids inheriting a default key ring created by another Windows user.
if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();

    var dataProtection = builder.Services.AddDataProtection()
        .SetApplicationName("RaceDay.Api");

    if (OperatingSystem.IsWindows())
    {
        var keyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RaceDay", "Development", "DataProtection", "Api");

        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
            .ProtectKeysWithDpapi(protectToLocalMachine: false);
    }
}


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Store session data in memory during development.
builder.Services.AddDistributedMemoryCache();

// Configure how long a logged-in session can remain idle.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    // JavaScript running in the browser cannot read this cookie.
    options.Cookie.HttpOnly = true;

    // Allow the cookie to be used during local development.
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".RaceDay.Session";
});

// Use ASP.NET Core's password hasher for RaceDay user accounts.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();



// Tell the application to use SQL Server for RaceDay data.
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

if (args.Contains("--provision-organiser", StringComparer.Ordinal))
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine(
            "Organiser provisioning is available only in the Development environment.");
        Environment.ExitCode = 1;
        return;
    }

    await DevelopmentOrganiserProvisioner.RunAsync(app.Services);
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Read session information before the request reaches a controller.
app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Allows the automated test project to access the API entry point.
public partial class Program { }
