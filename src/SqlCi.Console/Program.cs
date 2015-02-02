using Mono.Options;
using Newtonsoft.Json.Linq;
using SqlCi.ScriptRunner;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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
                    {"d|deploy", "deploy the database. Usage: d <environment>", p => config.Deploy = p != null},
                    {"h|help", "show this message and exit", p => config.ShowHelp = p != null},
                    {"v|version", "show the version number", p => config.ShowVersionNumber = p != null},
                    {"sh|showHistory", "show the history of scripts ran against the database", p => config.ShowHistory = p != null},
                    {"g|generateScript", "generates a new script file. Usage: g <environment> <script_name> <script_folder>", p => config.GenerateScript = p != null}
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

        private static void GetConfigurationValues(ConfigurationValues config, string environment, bool generateOnly)
        {
            // verify that the config file exists
            if (!File.Exists("config.json"))
            {
                throw new FileNotFoundException("Couldn't open config.json file. Make sure the file exists and then try again.");
            }

            // load the configuration file
            dynamic configuration = JObject.Parse(File.ReadAllText("config.json"));

            // verify that the target environment configuration exists
            if (null == configuration[environment])
            {
                throw new ConfigurationErrorsException(string.Format("There are no values for the \"{0}\" environment in the config.json file.", environment));
            }

            // load the config values
            if (!generateOnly)
            {
                config.ConnectionString = configuration[environment].connectionString.Value;
                config.ResetDatabase = bool.Parse(configuration[environment].resetDatabase.Value);
            }

            config.ScriptsFolder = configuration.common.scriptsFolder.Value;
            config.ResetScriptsFolder = configuration.common.resetScriptsFolder.Value;
            config.ReleaseVersion = configuration.common.releaseVersion.Value;
            config.ScriptTable = configuration.common.scriptTable.Value;
            config.Environment = environment;

            if (!generateOnly && config.ResetDatabase)
            {
                config.ResetConnectionString = configuration[environment].resetConnectionString.Value;
            }
        }

        private static int Main(string[] args)
        {
            // parse the arguments
            ConfigurationValues config;
            var optionSet = DefineOptionSet(args, out config);

            if (null == optionSet)
            {
                return -1;
            }

            // if there are no arguments then show the help and exit
            if (config.ShowHelp || args.Length == 0)
            {
                ShowHelp(optionSet);
                return 0;
            }

            // if the user wants the version number
            if (config.ShowVersionNumber)
            {
                ShowVersion();
                return 0;
            }

            try
            {
                // we need to read in the config.json file here because all other options requires
                // that we know the config
                GetConfigurationValues(config, args[1], config.GenerateScript);

                // if the user wants to generate a script file
                if (config.GenerateScript)
                {
                    var fileName = string.Format("{0}_{1}_{2}.sql", DateTime.Now.ToString("yyyyMMddHHmmssfff"), config.Environment, args[2]);
                    File.CreateText(string.Format(@"{0}\{1}", config.ScriptsFolder, fileName));
                    return 0;
                }

                // create a script configuration
                var scriptConfiguration = new ScriptConfiguration()
                 .WithConnectionString(config.ConnectionString)
                 .WithScriptsFolder(config.ScriptsFolder).WithResetDatabase(config.ResetDatabase)
                 .WithResetFolder(config.ResetScriptsFolder).WithReleaseNumber(config.ReleaseVersion)
                 .WithScriptTable(config.ScriptTable).WithEnvironment(config.Environment)
                 .WithResetConnectionString(config.ResetConnectionString).Verify();

                var executor = new Executor();

                // write any status updates that the executor sends to the console
                executor.StatusUpdate += (sender, @event) => System.Console.WriteLine(@event.Status);

                if (config.ShowHistory)
                {
                    var runHistory = executor.GetHistory(scriptConfiguration);
                    ShowHistory(runHistory); return 0;
                }

                var executionResults = executor.Execute(scriptConfiguration);

                // if we were successful return 0
                if (executionResults.WasSuccessful) { return 0; }

                // otherwise return -1 to signal an error
                return -1;
            }
            catch (Exception ex)
            {
                ShowConsoleError(ex.GetBaseException().Message);
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

        private static void ShowHistory(IEnumerable<Script> runHistory)
        {
            // sort the list by date & name
            var sortedList = runHistory.OrderBy(s => s.AppliedOnUtc).ThenBy(s => s.Name).ToList();

            // print the headers
            System.Console.WriteLine();
            System.Console.WriteLine("Version\t\tDate Ran\t\t\tScript Name");
            System.Console.WriteLine("=======\t\t========\t\t\t===========");

            // if no scripts have been ran then nothing to do.
            if (sortedList.Count == 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Current Database Version: 0.0.0 - No script files have been ran.");
            }

            // print the scripts
            foreach (var script in sortedList)
            {
                System.Console.WriteLine("{0}\t\t{1}\t\t{2}", script.Release, script.AppliedOnUtc.ToLocalTime(), script.Name);
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Current Database Version: {0} ({1})", sortedList.Last().Release, sortedList.Last().AppliedOnUtc.ToLocalTime());
        }

        private static void ShowVersion()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;

            System.Console.WriteLine("SqlCi.Console: {0}", string.Concat(version.Major, ".", version.Minor, ".", version.Build));
        }
    }
}