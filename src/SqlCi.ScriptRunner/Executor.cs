using SqlCi.ScriptRunner.Events;
using SqlCi.ScriptRunner.Providers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlCi.ScriptRunner
{
    public class Executor
    {
        private readonly IDatabaseProvider _databaseProvider;

        private Configuration _configuration = null!;
        private string _database = null!;
        private EnvironmentConfiguration _environmentConfiguration = null!;
        private IDbConnection? _connection;

        public event EventHandler<StatusUpdateEvent>? StatusUpdate;

        /// <summary>
        /// Creates a new Executor using the specified database provider.
        /// </summary>
        public Executor(IDatabaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
        }

        /// <summary>
        /// Creates a new Executor defaulting to SQL Server (for backward compatibility during transition).
        /// </summary>
        public Executor() : this(new SqlServerProvider())
        {
        }

        public async Task<ExecutionResults> ExecuteAsync(Configuration configuration, string environment)
        {
            VerifyConfiguration(configuration, environment);

            if (_environmentConfiguration.ResetDatabase)
            {
                LogInfo("Resetting the database ...");
                await ResetAsync();
            }

            LogInfo($"Deploying version {configuration.Version} to {_environmentConfiguration.Name}");
            var results = await DeployAsync();

            LogSuccess("Deployment Complete.");
            return results;
        }

        public async Task<IEnumerable<Script>> GetHistoryAsync(Configuration configuration, string environment)
        {
            VerifyConfiguration(configuration, environment);

            var tableExisted = await EnsureTrackingTableExistsAsync();

            var runHistory = new List<Script>();

            if (!tableExisted)
            {
                return runHistory;
            }

            LogInfo("Reading script run history ...");

            OpenConnection();

            try
            {
                var historyRecords = await _databaseProvider.GetScriptExecutionHistoryAsync(_connection!, _configuration.ScriptTable);

                foreach (var record in historyRecords)
                {
                    runHistory.Add(new Script(record.Id, record.Script, record.Release, record.AppliedOnUtc));
                }
            }
            finally
            {
                CloseConnection();
            }

            return runHistory;
        }

        protected virtual void OnRaiseStatusUpdateEvent(StatusUpdateEvent e)
        {
            // Make a temporary copy of the event to avoid possibility of a race condition if the last subscriber unsubscribes immediately after the
            // null check and before the event is raised.
            StatusUpdate?.Invoke(this, e);
        }

        private async Task AuditScriptRanAsync(string sqlScriptFileName)
        {
            OpenConnection();

            try
            {
                var id = ExtractIdFromFileName(sqlScriptFileName);
                await _databaseProvider.RecordScriptRunAsync(
                    _connection!,
                    _configuration.ScriptTable,
                    id,
                    sqlScriptFileName,
                    _configuration.Version,
                    DateTime.UtcNow);
            }
            finally
            {
                CloseConnection();
            }
        }

        private void CloseConnection()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                LogInfo("Closing connection to database ...");
                _connection.Close();
            }
        }

        private async Task CreateTrackingTableAsync()
        {
            OpenConnection();

            try
            {
                await _databaseProvider.EnsureTrackingTableExistsAsync(_connection!, _configuration.ScriptTable);
                LogSuccess("Script tracking table was created ...");
            }
            finally
            {
                CloseConnection();
            }
        }

        private async Task<ExecutionResults> DeployAsync()
        {
            LogInfo($"Loading change script(s) from {_configuration.ScriptsFolder} ...");
            var allScriptFiles = LoadSqlScriptFiles(_configuration.ScriptsFolder);
            LogSuccess($"Loaded {allScriptFiles.Count} change script(s) from {_configuration.ScriptsFolder} ...");

            if (allScriptFiles.Count == 0)
            {
                LogWarning("No change script(s) to execute.");
                return new ExecutionResults(true);
            }

            foreach (var scriptFile in allScriptFiles)
            {
                LogInfo($"\t{scriptFile}");
            }

            try
            {
                LogInfo("Checking for existance of script tracking table in the database ...");

                var tableExisted = await EnsureTrackingTableExistsAsync();

                List<string> sqlScriptFiles;
                if (tableExisted)
                {
                    LogInfo("Checking to see which change script(s) have already been applied ...");
                    var ranScriptFiles = await GetRanScriptsAsync();
                    LogInfo($"Found {ranScriptFiles.Count} change script(s) that have already been applied ...");

                    foreach (var scriptFile in ranScriptFiles)
                    {
                        LogInfo($"\t{scriptFile}");
                    }

                    LogInfo("Calculating which new change script(s) need to be applied ...");
                    sqlScriptFiles = allScriptFiles.Except(ranScriptFiles).ToList();
                    LogInfo($"{sqlScriptFiles.Count} new change script(s) need to be applied ...");
                }
                else
                {
                    sqlScriptFiles = allScriptFiles;
                    LogWarning($"{sqlScriptFiles.Count} new script(s) need to be applied ...");
                }

                foreach (var sqlScriptFileName in sqlScriptFiles)
                {
                    LogInfo($"\tApplying change script {sqlScriptFileName} ...");
                    await RunScriptFileAsync(_configuration.ScriptsFolder, sqlScriptFileName);
                    await AuditScriptRanAsync(sqlScriptFileName);
                }

                return new ExecutionResults(true);
            }
            finally
            {
                CloseConnection();
            }
        }

        private async Task<bool> DoesTrackingTableExistAsync()
        {
            OpenConnection();

            try
            {
                return await _databaseProvider.TrackingTableExistsAsync(_connection!, _configuration.ScriptTable);
            }
            finally
            {
                CloseConnection();
            }
        }

        private async Task<bool> EnsureTrackingTableExistsAsync()
        {
            var exists = await DoesTrackingTableExistAsync();

            if (!exists)
            {
                LogWarning("Script tracking table did not exist. Creating it now ...");
                await CreateTrackingTableAsync();
            }
            else
            {
                LogInfo("Script tracking table already exists ...");
            }

            return exists;
        }

        private string ExtractIdFromFileName(string sqlScriptFileName)
        {
            // get just the file name and not the full path
            var fileName = Path.GetFileName(sqlScriptFileName);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ApplicationException($"The file {sqlScriptFileName} does not exist.");
            }

            // take anything left of the first underscore
            var underscoreIndex = fileName.IndexOf('_');
            return fileName.Substring(0, underscoreIndex);
        }

        private async Task<List<string>> GetRanScriptsAsync()
        {
            var ranScripts = new List<string>();

            OpenConnection();

            try
            {
                var applied = await _databaseProvider.GetAppliedScriptsAsync(_connection!, _configuration.ScriptTable);
                ranScripts.AddRange(applied);
            }
            finally
            {
                CloseConnection();
            }

            return ranScripts;
        }

        private List<string> LoadSqlScriptFiles(string directory, bool loadAll = false)
        {
            // we want to load scripts that start with <sequence>_all_*.sql or <sequence>_<environment>_*.sql
            const string allRegex = @"[0-9]+_all_.*";
            var envRegex = $"[0-9]+_{_environmentConfiguration.Name}_.*";

            var filesPaths = Directory.GetFiles(directory, "*.sql");

            return loadAll 
                ? filesPaths.Select(Path.GetFileName).Where(fn => fn is not null).Select(fn => fn!).ToList() 
                : filesPaths.Select(Path.GetFileName).Where(fn => fn is not null && (Regex.IsMatch(fn, allRegex, RegexOptions.IgnoreCase) || Regex.IsMatch(fn, envRegex, RegexOptions.IgnoreCase))).Select(fn => fn!).ToList();
        }

        private void LogError(string msg)
        {
            var sanitizedMsg = RedactSecrets(msg);
            OnRaiseStatusUpdateEvent(new StatusUpdateEvent(sanitizedMsg, StatusLevelEnum.Error));
        }

        private void LogInfo(string msg)
        {
            var sanitizedMsg = RedactSecrets(msg);
            OnRaiseStatusUpdateEvent(new StatusUpdateEvent(sanitizedMsg, StatusLevelEnum.Info));
        }

        private void LogSuccess(string msg)
        {
            var sanitizedMsg = RedactSecrets(msg);
            OnRaiseStatusUpdateEvent(new StatusUpdateEvent(sanitizedMsg, StatusLevelEnum.Success));
        }

        private void LogWarning(string msg)
        {
            var sanitizedMsg = RedactSecrets(msg);
            OnRaiseStatusUpdateEvent(new StatusUpdateEvent(sanitizedMsg, StatusLevelEnum.Warning));
        }

        private void OpenConnection(bool useResetConnection = false)
        {
            if (_connection == null)
            {
                var connString = useResetConnection 
                    ? _environmentConfiguration.ResetConnectionString 
                    : _environmentConfiguration.ConnectionString;

                if (string.IsNullOrWhiteSpace(connString))
                    throw new InvalidOperationException("Connection string is not configured.");

                _connection = _databaseProvider.CreateConnection(connString);
            }

            if (_connection.State == ConnectionState.Closed)
            {
                var connString = useResetConnection 
                    ? _environmentConfiguration.ResetConnectionString 
                    : _environmentConfiguration.ConnectionString;

                LogInfo($"Opening connection to database using connection string: {RedactSecrets(connString ?? string.Empty)} ...");
                _connection.ConnectionString = connString!;
                _connection.Open();

                if (!useResetConnection && _connection is { Database: not null })
                {
                    _database = _connection.Database;
                }
            }
        }

        private string RedactSecrets(string msg)
        {
            // We want to redact passwords from connection strings before raising the event so that they won't be logged anywhere
            const string pattern = @"password\s?=(.+);";

            if (Regex.IsMatch(msg, pattern, RegexOptions.IgnoreCase))
            {
                return Regex.Replace(msg, pattern, "password=xxxxxx;");
            }

            return msg;
        }

        private async Task ResetAsync()
        {
            LogInfo($"Loading reset script(s) from {_configuration.ResetScriptsFolder} ...");
            var resetScripts = LoadSqlScriptFiles(_configuration.ResetScriptsFolder);
            LogInfo($"Loaded {resetScripts.Count} reset script(s) from {_configuration.ResetScriptsFolder} ...");

            if (resetScripts.Count == 0)
            {
                LogWarning("No reset script(s) to execute.");
                return;
            }

            foreach (var scriptFile in resetScripts)
            {
                LogInfo($"\t{scriptFile}");
            }

            LogWarning("Resetting Database ...");
            foreach (var sqlScriptFileName in resetScripts)
            {
                LogWarning($"\tApplying reset script {sqlScriptFileName} ...");
                await RunScriptFileAsync(_configuration.ResetScriptsFolder, sqlScriptFileName, true);
            }

            LogSuccess("Database reset complete.");

            CloseConnection();
        }

        private async Task RunScriptFileAsync(string folder, string sqlScriptFileName, bool resettingDatabase = false)
        {
            OpenConnection(resettingDatabase);

            try
            {
                var sqlText = File.ReadAllText(Path.Combine(folder, sqlScriptFileName));
                await _databaseProvider.ExecuteScriptAsync(_connection!, sqlText);
            }
            finally
            {
                CloseConnection();
            }
        }

        private void VerifyConfiguration(Configuration configuration, string environment)
        {
            // let our host know we are about to verify the config
            LogInfo("Verifying configuration ...");

            // no config means we can't proceed
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            // make sure that the host called Verify() on the environment before we try to do any work
            var environmentConfig = configuration.Verify(environment);

            // let our host know configuration has been verified
            LogSuccess("Configuration verification complete.");

            // store the config so other instance methods can reference it later on
            _configuration = configuration;
            _environmentConfiguration = environmentConfig;
        }
    }
}