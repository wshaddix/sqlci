using System;

namespace SqlCi.ScriptRunner.Providers;

public static class DatabaseProviderFactory
{
    /// <summary>
    /// Creates an IDatabaseProvider based on the provider name from configuration.
    /// </summary>
    /// <param name="providerName">Case-insensitive. Supported: "SqlServer", "PostgreSql", "Sqlite"</param>
    public static IDatabaseProvider Create(string providerName)
    {
        return providerName?.ToLowerInvariant() switch
        {
            "sqlserver" or "mssql" or "sql server" => new SqlServerProvider(),
            "postgresql" or "postgres" or "pgsql"   => new PostgreSqlProvider(),
            "sqlite"                                 => new SqliteProvider(),
            _ => throw new NotSupportedException(
                $"Unsupported DbProvider '{providerName}'. " +
                "Supported values are: SqlServer, PostgreSql, Sqlite.")
        };
    }
}
