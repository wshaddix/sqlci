using Mono.Options;
using SqlCi.ScriptRunner;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace SqlCi.Console
{
    internal class Program
    {
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
                    {"ev|environment=", "The environment that the scripts are being ran in",  p => config.Environment = p},
                    {"rd|resetDatabase", "Determines if the database should be reset", p => config.ResetDatabase = p != null},
                    {"rf|resetFolder=", "The folder that holds the database reset scripts to be ran if resetDatabase is specified", p => config.ResetFolder = p},
                    {"rc|resetConnectionString=", "The connectoin string to use to reset the database", p => config.ResetConnectionString = p},
                    {"h|help", "show this message and exit", p => config.ShowHelp = p != null},
                    {"v|version", "show the version number", p => config.VersionNumber = p != null},
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

        private static ConfigurationValues GetConfigurationValues(ConfigurationValues config)
        {
            config.ConnectionString = GetConnectionString();
            config.ScriptsFolder = GetScriptsFolder();
            config.ResetDatabase = GetResetDatabase();
            if (config.ResetDatabase)
            {
                config.ResetFolder = GetResetFolder();
                config.ResetConnectionString = GetResetConnectionString();
            }
            config.ReleaseNumber = GetReleaseNumber();
            config.ScriptTable = GetScriptTable();
            config.Environment = GetEnvironment();
            return config;
        }

        private static string GetConnectionString()
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["Database"];

            if (null == connectionStringSettings)
            {
                throw new ApplicationException("There is no connection string named 'Database' in the configuration file.");
            }

            // grab the connection string
            string connectionString = connectionStringSettings.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                System.Console.WriteLine("The connectionString attribute of the 'Database' connection string in the configuration file is empty");
                return string.Empty;
            }

            return connectionString;
        }

        private static string GetEnvironment()
        {
            return ConfigurationManager.AppSettings["Environment"];
        }

        private static string GetReleaseNumber()
        {
            return ConfigurationManager.AppSettings["ReleaseVersion"];
        }

        private static string GetResetConnectionString()
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["ResetDatabase"];

            if (null == connectionStringSettings)
            {
                throw new ApplicationException("There is no connection string named 'ResetDatabase' in the configuration file.");
            }

            // grab the connection string
            string connectionString = connectionStringSettings.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
            {
                System.Console.WriteLine("The connectionString attribute of the 'ResetDatabase' connection string in the configuration file is empty");
                return string.Empty;
            }

            return connectionString;
        }

        private static bool GetResetDatabase()
        {
            bool resetDatabase;
            bool.TryParse(ConfigurationManager.AppSettings["ResetDatabase"], out resetDatabase);
            return resetDatabase;
        }

        private static string GetResetFolder()
        {
            return ConfigurationManager.AppSettings["ResetFolder"];
        }

        private static string GetScriptsFolder()
        {
            return ConfigurationManager.AppSettings["ScriptsFolder"];
        }

        private static string GetScriptTable()
        {
            return ConfigurationManager.AppSettings["ScriptTable"];
        }

        private static int Main(string[] args)
        {
            // if we only have one arg and it is 'g' then the user just wants to generate a new sql
            // script file
            if (args.Length == 4 && args[0].StartsWith("g"))
            {
                var fileName = string.Format("{0}_{1}_{2}.sql", DateTime.Now.ToString("yyyyMMddHHmmssfff"), args[1], args[2]);
                File.CreateText(string.Format(@"{0}\{1}", args[3], fileName));
                return 0;
            }

            ConfigurationValues config;
            var optionSet = DefineOptionSet(args, out config);

            if (null == optionSet)
            {
                return -1;
            }

            if (config.ShowHelp || args.Length == 0)
            {
                ShowHelp(optionSet);
                return 0;
            }

            if (config.VersionNumber)
            {
                ShowVersion();
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
                    .WithEnvironment(config.Environment)
                    .WithResetConnectionString(config.ResetConnectionString)
                    .Verify();

                var executor = new Executor();

                // write any status updates that the executor sends to the console
                executor.StatusUpdate += (sender, @event) => System.Console.WriteLine(@event.Status);

                // run the scripts
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

        private static void ShowConsoleError(string msg)
        {
            System.Console.WriteLine("ERROR: {0}", msg);
        }

        private static void ShowHelp(OptionSet optionSet)
        {
            System.Console.WriteLine("Usage: SqlCi.Console [OPTIONS]");
            System.Console.WriteLine("Runs a set of sql scripts against the specified database.");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(System.Console.Out);
        }

        private static void ShowVersion()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;

            System.Console.WriteLine(string.Format("SqlCi.Console: {0}", string.Concat(version.Major, ".", version.Minor, ".", version.Build)));
        }
    }
}