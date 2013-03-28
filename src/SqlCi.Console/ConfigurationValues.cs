namespace SqlCi.Console
{
    internal class ConfigurationValues
    {
        internal string ConnectionString { get; set; }
        internal string ScriptsFolder { get; set; }
        internal bool ResetDatabase { get; set; }
        internal string ResetFolder { get; set; }
        internal string ReleaseNumber { get; set; }
        internal string ScriptTable { get; set; }
    }
}