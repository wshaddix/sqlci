using Mono.Options;
using SqlCi.ScriptRunner;
using System;
using System.Configuration;

namespace SqlCi.Console
{
    class Program
    {
        static int Main(string[] args)
        {
            ConfigurationValues config;
            var optionSet = DefineOptionSet(args, out config);

            if (null == optionSet)
            {
                return -1;
            }

            if (config.ShowHelp)
            {
                ShowHelp(optionSet);
                return 0;
            }

            try
            {
                // grab the configuration values from the .config file if the user specified to
                if (config.UseConfigFile)
                {
                    GetConfigurationValues(config);
                }

                // create a script configuration
                var scriptConfiguration = new ScriptConfiguration()
                    .WithConnectionString(config.ConnectionString)
                    .WithScriptsFolder(config.ScriptsFolder)
                    .WithResetDatabase(config.ResetDatabase)
                    .WithResetFolder(config.ResetFolder)
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

        private static OptionSet DefineOptionSet(string[] args, out ConfigurationValues configuration)
        {
            var config = new ConfigurationValues();

            var optionSet = new OptionSet()
                {
                    {"uc|useConfig", "Determines whether to get config values from the SqlCi.Console.exe.config file or the command line arguments", p => config.UseConfigFile = p != null},
                    {"cs|connectionString=", "The connection string to use to access the database to run the scripts in", p => config.ConnectionString = p },
                    {"st|scriptTable=", "The name of the script table", p => config.ScriptTable = p },
                    {"rv|releaseVersion=", "The version of this release",  p => config.ReleaseNumber = p},
                    {"sf|scriptsFolder=", "The folder that holds the sql scripts to be ran",  p => config.ScriptsFolder = p},
                    {"rd|resetDatabase", "Determines if the database should be reset", p => config.ResetDatabase = p != null},
                    {"rf|resetFolder=", "The folder that holds the database reset scripts to be ran if resetDatabase is specified", p => config.ResetFolder = p},
                    {"h|help", "show this message and exit", p => config.ShowHelp = p != null}
                };

            try
            {
                optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                System.Console.Write("SqlCi.Console: ");
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine("Try 'SqlCi.Console --help' for more information.");
                configuration = null;
                return null;
            }

            configuration = config;
            return optionSet;
        }

        private static void ShowHelp(OptionSet optionSet)
        {
            System.Console.WriteLine("Usage: SqlCi.Console [OPTIONS]");
            System.Console.WriteLine("Runs a set of sql scripts against the specified database.");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(System.Console.Out);
        }

        private static ConfigurationValues GetConfigurationValues(ConfigurationValues config)
        {
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
