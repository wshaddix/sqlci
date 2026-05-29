using System.Data;
using System.Text.RegularExpressions;

namespace SqlCi.ScriptRunner.Providers;

/// <summary>
/// Shared helpers for database providers: connection casting and identifier validation.
/// </summary>
internal static class ProviderHelpers
{
    // A safe, unquoted SQL identifier: starts with a letter or underscore, followed by
    // letters, digits, or underscores. Intentionally strict to prevent SQL injection,
    // since table names are interpolated directly into DDL/DML.
    private static readonly Regex IdentifierPattern =
        new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// Casts the connection to the concrete type expected by the provider, throwing a
    /// clear exception when the wrong connection type is supplied.
    /// </summary>
    public static TConnection Cast<TConnection>(IDbConnection connection)
        where TConnection : class, IDbConnection
        => connection as TConnection
           ?? throw new ArgumentException(
               $"Connection must be a {typeof(TConnection).Name} for this provider.",
               nameof(connection));

    /// <summary>
    /// Validates that a table name is a safe SQL identifier and returns it unchanged.
    /// Throws <see cref="ArgumentException"/> when the name is unsafe to interpolate.
    /// </summary>
    public static string ValidateTableName(string tableName)
    {
        if (!IsValidIdentifier(tableName))
        {
            throw new ArgumentException(
                $"Invalid table name '{tableName}'. Table names must start with a letter or " +
                "underscore and contain only letters, digits, and underscores.",
                nameof(tableName));
        }

        return tableName;
    }

    /// <summary>
    /// Returns true when the supplied name is a safe, unquoted SQL identifier.
    /// </summary>
    public static bool IsValidIdentifier(string? name)
        => !string.IsNullOrWhiteSpace(name) && IdentifierPattern.IsMatch(name);
}
