using App.DAL.EF;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Tests.Integration;

public class BeautyBookFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "IntegrationTest_" + Guid.NewGuid().ToString("N");
    private SqliteConnection? _masterConnection;

    private string ConnectionString => $"DataSource={_dbName};Mode=Memory;Cache=Shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Keep the named in-memory database alive for the duration of the test run.
        _masterConnection = new SqliteConnection(ConnectionString);
        _masterConnection.Open();

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Key"] = TestJwtHelper.SigningKey,
                ["JWT:Issuer"] = TestJwtHelper.Issuer,
                ["JWT:Audience"] = TestJwtHelper.Audience,
                ["DataInitialization:DropDatabase"] = "false",
                ["DataInitialization:MigrateDatabase"] = "false",
                ["DataInitialization:SeedIdentity"] = "false",
                ["DataInitialization:SeedData"] = "false",
                ["ConnectionStrings:DefaultConnection"] = ConnectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing Npgsql DbContext registration (options + any options-configuration lambdas).
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            var toRemove = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.GetGenericTypeDefinition().FullName?
                                .Contains("IDbContextOptionsConfiguration") == true)
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(ConnectionString)
                       .AddInterceptors(new DisableSqliteFkInterceptor()));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Create the SQLite schema before any test runs.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _masterConnection?.Dispose();
        }
        base.Dispose(disposing);
    }

    public AppDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
