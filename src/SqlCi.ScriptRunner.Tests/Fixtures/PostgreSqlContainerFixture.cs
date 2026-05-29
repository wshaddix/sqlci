using Npgsql;
using Testcontainers.PostgreSql;

namespace SqlCi.ScriptRunner.Tests.Fixtures;

/// <summary>
/// Manages a shared PostgreSQL container for integration tests using Testcontainers.
/// The container is started lazily on first access.
/// </summary>
public sealed class PostgreSqlContainerFixture : IAsyncDisposable
{
    private PostgreSqlContainer? _container;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _started;

    public string? ConnectionString { get; private set; }

    public bool IsAvailable => _container is not null && _started;

    public async Task EnsureStartedAsync()
    {
        if (_started) return;

        await _lock.WaitAsync();
        try
        {
            if (_started) return;

            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("sqlci_test")
                .WithUsername("sqlci")
                .WithPassword("sqlci_test_password")
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();

            ConnectionString = _container.GetConnectionString();

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            _started = true;
        }
        catch
        {
            _container = null;
            ConnectionString = null;
            _started = false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
        _lock.Dispose();
    }
}
