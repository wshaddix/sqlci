using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace SqlCi.ScriptRunner.Tests.Fixtures;

/// <summary>
/// Manages a shared SQL Server container for integration tests using Testcontainers.
/// The container is started lazily on first access.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncDisposable
{
    private MsSqlContainer? _container;
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

            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("YourStrong!Passw0rd")
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();

            ConnectionString = _container.GetConnectionString();

            // Quick connection test
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            _started = true;
        }
        catch
        {
            // Docker not available or startup failed.
            _container = null;
            ConnectionString = null;
            _started = false;
            // Swallow - tests will skip themselves
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
