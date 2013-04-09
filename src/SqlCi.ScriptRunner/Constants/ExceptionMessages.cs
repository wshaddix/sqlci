namespace SqlCi.ScriptRunner.Constants
{
    internal static class ExceptionMessages
    {
        internal static string MissingConnectionString = "Connection string cannot be blank.";
        internal static string MissingScriptsFolder = "Scripts folder cannot be blank.";
        internal static string MissingResetFolder = "Rest folder cannot be blank if you specify ResetDatabase(true).";
        internal static string MissingReleaseNumber = "Release number cannot be blank.";
        internal static string MissingScriptTable = "Script table cannot be blank.";
        internal static string NotVerified = "You cannot call Execute() before calling Verify().";
        internal static string ScriptsFolderDoesNotExist = "Scripts folder does not exist.";
        internal static string ResetFolderDoesNotExist = "Reset scripts folder does not exist.";
        internal static string MissingEnvironment = "Environment cannot be blank.";
    }
}