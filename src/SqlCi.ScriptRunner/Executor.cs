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
    public class Executor : IDisposable
    {
        private readonly IDatabaseProvider _databaseProvider;

        private Configuration _configuration = null!;
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

            LogInfo($"Deploying version {_configuration.Version} to {_environmentConfiguration.Name}");
            var results = await DeployAsync();

            LogSuccess("Deployment Complete.");
            return results;
        }

        public async Task<IEnumerable<Script>> GetHistoryAsync(Configuration configuration, string environment)
        {
            VerifyConfiguration(configuration, environment);

            var runHistory = new List<Script>();

            OpenConnection();

            try
            {
                // Reading history must never create the tracking table as a side effect.
                var exists = await _databaseProvider.TrackingTableExistsAsync(_connection!, _configuration.ScriptTable);

                if (!exists)
                {
                    return runHistory;
                }

                LogInfo("Reading script run history ...");

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

            OpenConnection();

            try
            {
                LogInfo("Checking for existance of script tracking table in the database ...");

                var tableExisted = await EnsureTrackingTableExistsAsync();

                List<string> sqlScriptFiles;
                if (tableExisted)
                {
                    LogInfo("Checking to see which change script(s) have already been applied ...");
                    var ranScriptFiles = (await _databaseProvider.GetAppliedScriptsAsync(_connection!, _configuration.ScriptTable)).ToList();
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
                    await ApplyScriptAsync(sqlScriptFileName);
                }

                return new ExecutionResults(true);
            }
            finally
            {
                CloseConnection();
            }
        }

        /// <summary>
        /// Applies a single change script and records it in the tracking table inside one
        /// transaction so the database is never left with an applied-but-unrecorded script.
        /// </summary>
        private async Task ApplyScriptAsync(string sqlScriptFileName)
        {
            var sqlText = File.ReadAllText(Path.Combine(_configuration.ScriptsFolder, sqlScriptFileName));
            var id = ExtractIdFromFileName(sqlScriptFileName);

            using var transaction = _connection!.BeginTransaction();

            try
            {
                await _databaseProvider.ExecuteScriptAsync(_connection!, sqlText, transaction);
                await _databaseProvider.RecordScriptRunAsync(
                    _connection!,
                    _configuration.ScriptTable,
                    id,
                    sqlScriptFileName,
                    _configuration.Version,
                    DateTime.UtcNow,
                    transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private async Task<bool> EnsureTrackingTableExistsAsync()
        {
            var exists = await _databaseProvider.TrackingTableExistsAsync(_connection!, _configuration.ScriptTable);

            if (!exists)
            {
                LogWarning("Script tracking table did not exist. Creating it now ...");
                await _databaseProvider.EnsureTrackingTableExistsAsync(_connection!, _configuration.ScriptTable);
                LogSuccess("Script tracking table was created ...");
            }
            else
            {
                LogInfo("Script tracking table already exists ...");
            }

            return exists;
        }

        private static string ExtractIdFromFileName(string sqlScriptFileName)
        {
            // get just the file name and not the full path
            var fileName = Path.GetFileName(sqlScriptFileName);

            // take anything left of the first underscore
            var underscoreIndex = fileName.IndexOf('_');

            if (underscoreIndex <= 0)
            {
                throw new ApplicationException(
                    $"The script file name '{fileName}' is invalid. Script files must follow the " +
                    "'<sequence>_<all|environment>_<description>.sql' naming convention.");
            }

            return fileName.Substring(0, underscoreIndex);
        }

        private List<string> LoadSqlScriptFiles(string directory)
        {
            // we want to load scripts that start with <sequence>_all_*.sql or <sequence>_<environment>_*.sql
            const string allRegex = @"[0-9]+_all_.*";
            var envRegex = $"[0-9]+_{_environmentConfiguration.Name}_.*";

            var filesPaths = Directory.GetFiles(directory, "*.sql");

            return filesPaths
                .Select(Path.GetFileName)
                .Where(fn => fn is not null
                             && (Regex.IsMatch(fn, allRegex, RegexOptions.IgnoreCase)
                                 || Regex.IsMatch(fn, envRegex, RegexOptions.IgnoreCase)))
                .Select(fn => fn!)
                .ToList();
        }

        private void LogInfo(string msg)
            => OnRaiseStatusUpdateEvent(new StatusUpdateEvent(RedactSecrets(msg), StatusLevelEnum.Info));

        private void LogSuccess(string msg)
            => OnRaiseStatusUpdateEvent(new StatusUpdateEvent(RedactSecrets(msg), StatusLevelEnum.Success));

        private void LogWarning(string msg)
            => OnRaiseStatusUpdateEvent(new StatusUpdateEvent(RedactSecrets(msg), StatusLevelEnum.Warning));

        private void OpenConnection(bool useResetConnection = false)
        {
            var connString = useResetConnection
                ? _environmentConfiguration.ResetConnectionString
                : _environmentConfiguration.ConnectionString;

            if (string.IsNullOrWhiteSpace(connString))
                throw new InvalidOperationException("Connection string is not configured.");

            if (_connection == null)
            {
                _connection = _databaseProvider.CreateConnection(connString);
            }

            if (_connection.State == ConnectionState.Closed)
            {
                LogInfo($"Opening connection to database using connection string: {RedactSecrets(connString)} ...");
                _connection.ConnectionString = connString;
                _connection.Open();
            }
        }

        private void CloseConnection()
        {
            if (_connection == null)
            {
                return;
            }

            if (_connection.State != ConnectionState.Closed)
            {
                LogInfo("Closing connection to database ...");
                _connection.Close();
            }

            _connection.Dispose();
            _connection = null;
        }

        private static string RedactSecrets(string msg) => SecretRedactor.Redact(msg);

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

            OpenConnection(useResetConnection: true);

            try
            {
                LogWarning("Resetting Database ...");

                // Reset scripts may contain statements (DROP/CREATE DATABASE) that cannot run inside a
                // transaction, so they are executed directly rather than transactionally.
                foreach (var sqlScriptFileName in resetScripts)
                {
                    LogWarning($"\tApplying reset script {sqlScriptFileName} ...");
                    var sqlText = File.ReadAllText(Path.Combine(_configuration.ResetScriptsFolder, sqlScriptFileName));
                    await _databaseProvider.ExecuteScriptAsync(_connection!, sqlText);
                }

                LogSuccess("Database reset complete.");
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

        public void Dispose()
        {
            CloseConnection();
            GC.SuppressFinalize(this);
        }
    }
}
