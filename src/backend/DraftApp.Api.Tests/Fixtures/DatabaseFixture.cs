using DraftApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DraftApp.Api.Tests.Fixtures;

/// <summary>
/// Fixture that provides in-memory database context for tests.
/// </summary>
public class DatabaseFixture : IDisposable
{
    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<DraftAppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new DraftAppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public DraftAppDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
