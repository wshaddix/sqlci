using System;

namespace SqlCi.ScriptRunner.Providers;

public static class DatabaseProviderFactory
{
    /// <summary>
    /// Normalizes a provider name (accepting common aliases) to its canonical form:
    /// "SqlServer", "PostgreSql", or "Sqlite". Throws <see cref="NotSupportedException"/>
    /// for unknown providers.
    /// </summary>
    /// <param name="providerName">Case-insensitive. Aliases such as "mssql" and "postgres" are accepted.</param>
    public static string Normalize(string? providerName)
    {
        return providerName?.Trim().ToLowerInvariant() switch
        {
            "sqlserver" or "mssql" or "sql server" => "SqlServer",
            "postgresql" or "postgres" or "pgsql" => "PostgreSql",
            "sqlite" => "Sqlite",
            _ => throw new NotSupportedException(
                $"Unsupported DbProvider '{providerName}'. " +
                "Supported values are: SqlServer, PostgreSql, Sqlite.")
        };
    }

    /// <summary>
    /// Creates an IDatabaseProvider based on the provider name from configuration.
    /// </summary>
    /// <param name="providerName">Case-insensitive. Supported: "SqlServer", "PostgreSql", "Sqlite"</param>
    public static IDatabaseProvider Create(string providerName)
    {
        return Normalize(providerName) switch
        {
            "SqlServer" => new SqlServerProvider(),
            "PostgreSql" => new PostgreSqlProvider(),
            "Sqlite" => new SqliteProvider(),
            // Normalize only ever returns the three canonical names above.
            _ => throw new NotSupportedException(providerName)
        };
    }
}
