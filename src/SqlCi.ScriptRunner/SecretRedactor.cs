using System.Text.RegularExpressions;

namespace SqlCi.ScriptRunner;

/// <summary>
/// Redacts secrets (currently passwords) from messages before they are logged or surfaced
/// via status events.
/// </summary>
internal static class SecretRedactor
{
    // Matches "Password=..." or "Pwd=..." (case-insensitive), with arbitrary whitespace and a
    // value that runs until the next ';' or end of string. Handles values with or without a
    // trailing semicolon.
    private static readonly Regex PasswordPattern =
        new(@"\b(password|pwd)\s*=\s*[^;]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Redact(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        return PasswordPattern.Replace(message, "$1=xxxxxx");
    }
}
