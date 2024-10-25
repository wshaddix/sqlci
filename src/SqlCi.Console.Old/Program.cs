using Mono.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlCi.ScriptRunner;
using SqlCi.ScriptRunner.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Configuration = SqlCi.ScriptRunner.Configuration;

namespace SqlCi.Console
{
    internal class Program
    {
        private static readonly string Version = Assembly.GetEntryAssembly().GetName().Version.ToString();
        private static Configuration _configuration;

        private static OptionSet DefineOptionSet(string[] args, out CommandLineOptions configuration)
        {
            var config = new CommandLineOptions();

            var optionSet = new OptionSet()
                {
                    {"i|init", "Initializes a new default config.json file and folders.\r\nUsage: -i <database>", p => config.Initialize = p != null},
                    {"h|history", "Show the history of scripts ran.\r\nUsage: -h <environment>", p => config.ShowHistory = p != null},
                    {"g|generate", "Generates a new script file.\r\nUsage: -g <environment> <script_name>", p => config.GenerateScript = p != null},
                    {"d|deploy", "Deploy the database.\r\nUsage: -d <environment>", p => config.Deploy = p != null},
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

        private static void GenerateScript(string environment, string scriptName)
        {
            var fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmssfff")}_{environment}_{scriptName}.sql";
            File.CreateText($@"{_configuration.ScriptsFolder}\{fileName}");
            System.Diagnostics.Process.Start($@"{_configuration.ScriptsFolder}\{fileName}");
        }

        private static void GetConfigurationValues(CommandLineOptions config, string environment, bool generateOnly)
        {
            // verify that the config file exists
            if (!File.Exists("config.json"))
            {
                throw new FileNotFoundException("Couldn't open config.json file. Make sure the file exists and then try again.");
            }

            // load the configuration file
            dynamic configuration = JObject.Parse(File.ReadAllText("config.json"));

            // verify that the target environment configuration exists. If the environment is "all" then ignore this because they are generating a script
            if (null == configuration[environment] && !environment.ToLowerInvariant().Equals("all"))
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

        private static void Initialize()
        {
            // if there is already a config file we should just exit
            if (File.Exists("config.json"))
            {
                throw new ApplicationException("config.json already exists.");
            }

            // create a configuration with defaults
            var configuration = new Configuration
            {
                ScriptsFolder = ".\\Scripts",
                ResetScriptsFolder = ".\\ResetScripts",
                ScriptTable = "ScriptTable",
                Version = "1.0.0",
            };
            configuration.Environments.Add(new EnvironmentConfiguration
            {
                Name = "local",
                ResetDatabase = true
            });

            // create the config.json file
            File.WriteAllText("config.json", JsonConvert.SerializeObject(configuration, Formatting.Indented));

            ShowStatusUpdate(new StatusUpdateEvent("Created config.json", StatusLevelEnum.Success));

            // ensure that the default Scripts folder exists
            if (!Directory.Exists("Scripts"))
            {
                Directory.CreateDirectory("Scripts");
                ShowStatusUpdate(new StatusUpdateEvent("Created Scripts directory", StatusLevelEnum.Success));
            }

            // ensure that hte default ResetScripts folder exists
            if (!Directory.Exists("ResetScripts"))
            {
                Directory.CreateDirectory("ResetScripts");
                ShowStatusUpdate(new StatusUpdateEvent("Created ResetScripts directory", StatusLevelEnum.Success));
            }

            // load up this default configuration
            _configuration = configuration;

            // create the first baseline script
            GenerateScript("all", "baseline");
            ShowStatusUpdate(new StatusUpdateEvent("Created baseline script in Scripts directory", StatusLevelEnum.Success));
        }

        private static void LoadAndVerifyConfig()
        {
            if (!File.Exists(@".\config.json"))
            {
                throw new ApplicationException("There is no config.json file. Run SqlCi.Console -i to get started.");
            }

            _configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(@".\config.json"));
            _configuration.Verify();
        }

        private static int Main(string[] args)
        {
            // parse the arguments
            CommandLineOptions commandLineOptions;
            var optionSet = DefineOptionSet(args, out commandLineOptions);

            if (null == optionSet)
            {
                return -1;
            }

            // if there are no arguments then show the help and exit
            if (args.Length == 0)
            {
                ShowHelp(optionSet);
                return 0;
            }

            try
            {
                // if we need to initialize
                if (commandLineOptions.Initialize)
                {
                    Initialize();
                    return 0;
                }

                // load the config.json file so we have our configuration. if it doesn't exist, tell the user to initialize it
                LoadAndVerifyConfig();

                // if the user wants to generate a script file
                if (commandLineOptions.GenerateScript)
                {
                    GenerateScript(args[1], args[2]);
                    return 0;
                }

                var executor = new Executor();

                // write any status updates that the executor sends to the console
                executor.StatusUpdate += (sender, @event) => ShowStatusUpdate(@event);

                if (commandLineOptions.ShowHistory)
                {
                    var runHistory = executor.GetHistory(_configuration, args[1]);
                    ShowHistory(runHistory); return 0;
                }

                var executionResults = executor.Execute(_configuration, args[1]);

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
            System.Console.WriteLine($"Version: {Version}");
            System.Console.WriteLine("Usage: SqlCi.Console [OPTIONS]");
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

        private static void ShowStatusUpdate(StatusUpdateEvent @event)
        {
            switch (@event.Level)
            {
                case StatusLevelEnum.Info:
                    System.Console.ForegroundColor = ConsoleColor.Gray;
                    break;

                case StatusLevelEnum.Success:
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case StatusLevelEnum.Warning:
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case StatusLevelEnum.Error:
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            System.Console.WriteLine(@event.Status);
            System.Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}