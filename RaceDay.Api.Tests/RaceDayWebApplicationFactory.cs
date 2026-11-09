using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class RaceDayWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeBlobStorageService BlobStorage { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            // TestServer has no Windows Event Log target; keep test diagnostics on the console.
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureServices(services =>
        {
            // Avoid reading or writing machine-level keys in the test host.
            services.AddDataProtection()
                .UseEphemeralDataProtectionProvider();

            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton(BlobStorage);
            services.AddSingleton<IBlobStorageService>(provider =>
                provider.GetRequiredService<FakeBlobStorageService>());

            // Remove the normal database registrations used by the API.
            services.RemoveAll<RaceDayDbContext>();
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();

            // Create one database name for this test host.
            var databaseName = $"RaceDayTests_{Guid.NewGuid():N}";

            // Every database context must use the same test database.
            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}
