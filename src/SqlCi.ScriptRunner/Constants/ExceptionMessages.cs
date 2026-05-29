namespace SqlCi.ScriptRunner.Constants;

internal static class ExceptionMessages
{
    internal const string MissingConnectionString = "Connection string \"Database\" cannot be blank.";
    internal const string MissingEnvironment = "Environment cannot be blank.";
    internal const string MissingReleaseNumber = "Release number cannot be blank.";
    internal const string MissingResetConnectionString = "Connection string \"ResetDatabase\" cannot be blank.";
    internal const string MissingScriptsFolder = "Scripts folder cannot be blank.";
    internal const string MissingScriptTable = "Script table cannot be blank.";
    internal const string InvalidScriptTable = "Script table must start with a letter or underscore and contain only letters, digits, and underscores.";
    internal const string ScriptsFolderDoesNotExist = "Scripts folder does not exist.";
}
