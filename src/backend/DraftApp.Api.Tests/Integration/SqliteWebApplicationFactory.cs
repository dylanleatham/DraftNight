using DraftApp.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DraftApp.Api.Tests.Integration;

/// <summary>
/// Hosts the API against a private in-memory SQLite database. Unlike the EF InMemory
/// provider, SQLite is relational, so raw SQL, transactions and concurrency tokens
/// behave as they do against SQL Server.
/// </summary>
public sealed class SqliteWebApplicationFactory : WebApplicationFactory<Program>
{
    // The in-memory database lives only as long as this connection stays open.
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    public SqliteWebApplicationFactory()
    {
        connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registrations so only one provider is configured
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<DraftAppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(DraftAppDbContext) ||
                    (d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("EntityFramework")) ||
                    (d.ImplementationType?.FullName != null && d.ImplementationType.FullName.Contains("SqlServer")))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DraftAppDbContext>(options => options.UseSqlite(connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<DraftAppDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
        }
    }
}
