namespace SqlCi.Console
{
    internal class ConfigurationValues
    {
        public bool Deploy { get; set; }

        internal string ConnectionString { get; set; }

        internal string Environment { get; set; }

        internal bool GenerateScript { get; set; }

        internal string ReleaseVersion { get; set; }

        internal string ResetConnectionString { get; set; }

        internal bool ResetDatabase { get; set; }

        internal string ResetScriptsFolder { get; set; }

        internal string RollBackToVersion { get; set; }

        internal string ScriptsFolder { get; set; }

        internal string ScriptTable { get; set; }

        internal bool ShowHelp { get; set; }

        internal bool ShowHistory { get; set; }

        internal bool ShowVersionNumber { get; set; }

        internal bool UseConfigFile { get; set; }
    }
}