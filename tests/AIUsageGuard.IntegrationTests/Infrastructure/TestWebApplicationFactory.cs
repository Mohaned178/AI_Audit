using AIUsageGuard.Application.Abstractions;
using AIUsageGuard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AIUsageGuard.IntegrationTests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly DatabaseFixture _databaseFixture = new();
    private bool _initialized;

    public async Task<T> ExecuteDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_databaseFixture.Connection)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        return await action(dbContext);
    }

    public async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_databaseFixture.Connection)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        await action(dbContext);
    }

    public async Task<HttpClient> CreateInitializedApiClientAsync()
    {
        if (!_initialized)
        {
            await _databaseFixture.InitializeAsync();
            _initialized = true;
        }

        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _databaseFixture.ConnectionString);
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = _databaseFixture.ConnectionString
            });
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await _databaseFixture.DisposeAsync();
        Dispose();
    }
}
