using System;
using SqlCi.ScriptRunner;
using System.Configuration;
using SqlCi.ScriptRunner.Events;

namespace SqlCi.Console
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                // grab the configuration values
                var config = GetConfigurationValues();

                // create a script configuration
                var scriptConfiguration = new ScriptConfiguration()
                    .WithConnectionString(config.ConnectionString)
                    .WithScriptsFolder(config.ScriptsFolder)
                    .ResetDatabase(config.ResetDatabase)
                    .ResetFolder(config.ResetFolder)
                    .LogToConsole(true)
                    .WithReleaseNumber(config.ReleaseNumber)
                    .WithScriptTable(config.ScriptTable)
                    .Verify();

                // execute the scripts
                var executor = new Executor();
                executor.StatusUpdate += (sender, @event) => System.Console.WriteLine(@event.Status);

                var executionResults = executor.Execute(scriptConfiguration);

                // if we were successful return 0
                if (executionResults.WasSuccessful)
                {
                    return 0;
                }

                // otherwise return -1 to signal an error
                return -1;
            }
            catch (Exception ex)
            {
                ShowConsoleError(ex.Message);
                return -1;
            }
        }

        private static ConfigurationValues GetConfigurationValues()
        {
            var config = new ConfigurationValues();
            config.ConnectionString = GetConnectionString();
            config.ScriptsFolder = GetScriptsFolder();
            config.ResetDatabase = GetResetDatabase();
            if (config.ResetDatabase)
            {
                config.ResetFolder = GetResetFolder();
            }
            config.ReleaseNumber = GetReleaseNumber();
            config.ScriptTable = GetScriptTable();
            return config;
        }

        private static string GetScriptTable()
        {
            return ConfigurationManager.AppSettings["ScriptTable"];
        }

        private static string GetReleaseNumber()
        {
            return ConfigurationManager.AppSettings["ReleaseVersion"];
        }

        private static string GetResetFolder()
        {
            return ConfigurationManager.AppSettings["ResetScriptsFolder"];
        }

        private static bool GetResetDatabase()
        {
            bool resetDatabase;
            bool.TryParse(ConfigurationManager.AppSettings["ResetDatabase"], out resetDatabase);
            return resetDatabase;
        }

        private static string GetScriptsFolder()
        {
            return ConfigurationManager.AppSettings["ScriptsFolder"];
        }

        private static string GetConnectionString()
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["Database"];

            if (null == connectionStringSettings)
            {
                throw new ApplicationException("ERROR: There is no connection string named 'Database' in the configuration file.");
            }

            // grab the connection string
            string connectionString = connectionStringSettings.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                System.Console.WriteLine("ERROR: The connectionString attribute of the 'Database' connection string in the configuration file is empty");
                return string.Empty;
            }

            return connectionString;
        }

        private static void ShowConsoleError(string msg)
        {
            System.Console.WriteLine("ERROR: {0}", msg);
        }
    }
}
