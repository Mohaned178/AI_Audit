using AIUsageGuard.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Infrastructure;

public sealed class DatabaseFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public DatabaseFixture()
    {
        _connection = new SqliteConnection($"Data Source=ai-usage-guard-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();
    }

    public string ConnectionString => _connection.ConnectionString;

    public SqliteConnection Connection => _connection;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _connection.DisposeAsync();
    }
}
