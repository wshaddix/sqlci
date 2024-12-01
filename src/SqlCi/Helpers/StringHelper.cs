using System.Text.RegularExpressions;

namespace SqlCi.Helpers;

public static class StringHelper
{
    public static string ToSafeFileName(string originalFileName)
    {
        // Define characters that are invalid in both Windows and Linux file names
        char[] invalidChars = Path.GetInvalidFileNameChars();

        // Regex pattern to replace invalid characters with an underscore
        string invalidCharsPattern = "[" + Regex.Escape(new string(invalidChars)) + "]";

        // Use Regex to replace invalid characters
        string safeFileName = Regex.Replace(originalFileName, invalidCharsPattern, "_");

        // Trim the name to a realistic file system length
        safeFileName = safeFileName.Length > 255 ? safeFileName[..255] : safeFileName;

        // Ensure file name is not empty
        return string.IsNullOrEmpty(safeFileName) ? "default" : safeFileName;
    }
}