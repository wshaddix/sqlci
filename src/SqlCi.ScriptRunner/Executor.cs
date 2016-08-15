using SqlCi.ScriptRunner.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlCi.ScriptRunner
{
    public class Executor
    {
        private Configuration _configuration;
        private string _database;
        private EnvironmentConfiguration _environmentConfiguration;
        private SqlConnection _sqlConnection;

        public event EventHandler<StatusUpdateEvent> StatusUpdate;

        public ExecutionResults Execute(Configuration configuration, string environment)
        {
            // ensure the configuration is valid for the given environment
            VerifyConfiguration(configuration, environment);

            // if we need to reset the database do that first
            if (_environmentConfiguration.ResetDatabase)
            {
                LogInfo("Resetting the database ...");
                Reset();
            }

            // deploy
            LogInfo($"Deploying version {configuration.Version} to {_environmentConfiguration.Name}");
            var results = Deploy();

            LogSuccess("Deployment Complete.");

            // return the results to our host
            return results;
        }

        public IEnumerable<Script> GetHistory(Configuration configuration, string environment)
        {
            VerifyConfiguration(configuration, environment);

            // ensure that the script table exists
            var tableExisted = EnsureScriptTableExists();

            var runHistory = new List<Script>();

            if (!tableExisted)
            {
                return runHistory;
            }

            LogInfo("Reading script run history ...");

            var sqlText = $"select Id, Script, Release, AppliedOnUtc from {_configuration.ScriptTable}";

            OpenSqlConnection();

            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        runHistory.Add(new Script((string)reader["Id"], (string)reader["Script"], (string)reader["Release"], (DateTime)reader["AppliedOnUtc"]));
                    }
                }
            }

            CloseSqlConnection();

            return runHistory;
        }

        protected virtual void OnRaiseStatusUpdateEvent(StatusUpdateEvent e)
        {
            // Make a temporary copy of the event to avoid possibility of a race condition if the last subscriber unsubscribes immediately after the
            // null check and before the event is raised.
            StatusUpdate?.Invoke(this, e);
        }

        private void AuditScriptRan(string sqlScriptFileName)
        {
            OpenSqlConnection();

            var id = ExtractIdFromFileName(sqlScriptFileName);

            var sqlText = $"insert into {_configuration.ScriptTable} (Id, Script, Release, AppliedOnUtc) values " + $"('{id}', '{sqlScriptFileName}', '{_configuration.Version}', '{DateTime.UtcNow}')";

            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;
                cmd.ExecuteNonQuery();
            }
        }

        private void CloseSqlConnection()
        {
            if (_sqlConnection.State != ConnectionState.Closed)
            {
                LogInfo("Closing connection to sql server ...");
                _sqlConnection.Close();
            }
        }

        private void CreateScriptTable()
        {
            var sqlText = $"create table {_configuration.ScriptTable} (Id nvarchar(50) not null constraint pk primary key clustered, Script nvarchar(255) not null, Release nvarchar(25) not null, AppliedOnUtc datetime not null)";
            OpenSqlConnection();
            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;
                cmd.ExecuteNonQuery();
            }
            LogSuccess("Script tracking table was created ...");
        }

        private ExecutionResults Deploy()
        {
            // load the sql scripts
            LogInfo($"Loading change script(s) from {_configuration.ScriptsFolder} ...");
            var allScriptFiles = LoadSqlScriptFiles(_configuration.ScriptsFolder);
            LogSuccess($"Loaded {allScriptFiles.Count} change script(s) from {_configuration.ScriptsFolder} ...");

            // if there are no scripts to run, just exit
            if (allScriptFiles.Count == 0)
            {
                LogWarning("No change script(s) to execute.");
                return new ExecutionResults(true);
            }

            // show which scripts were loaded
            foreach (var scriptFile in allScriptFiles)
            {
                LogInfo($"\t{scriptFile}");
            }

            try
            {
                LogInfo("Checking for existance of script tracking table in the database ...");

                // ensure that the script table exists
                var tableExisted = EnsureScriptTableExists();

                List<string> sqlScriptFiles;
                if (tableExisted)
                {
                    // find out which scripts have already been ran
                    LogInfo("Checking to see which change script(s) have already been applied ...");
                    var ranScriptFiles = GetRanScripts();
                    LogInfo($"Found {ranScriptFiles.Count()} change script(s) that have already been applied ...");

                    // show which scripts were applied
                    foreach (var scriptFile in ranScriptFiles)
                    {
                        LogInfo($"\t{scriptFile}");
                    }

                    // remove any scripts that have already ran
                    LogInfo("Calculating which new change script(s) need to be applied ...");
                    sqlScriptFiles = allScriptFiles.Except(ranScriptFiles).ToList();
                    LogInfo($"{sqlScriptFiles.Count} new change script(s) need to be applied ...");
                }
                else
                {
                    // if the script table didn't already exist then we need to run all of the sql scripts
                    sqlScriptFiles = allScriptFiles;
                    LogWarning($"{sqlScriptFiles.Count} new script(s) need to be applied ...");
                }

                // run the scripts
                foreach (var sqlScriptFileName in sqlScriptFiles)
                {
                    LogInfo($"\tApplying change script {sqlScriptFileName} ...");
                    RunScriptFile(_configuration.ScriptsFolder, sqlScriptFileName);
                    AuditScriptRan(sqlScriptFileName);
                }

                return new ExecutionResults(true);
            }
            finally
            {
                CloseSqlConnection();
            }
        }

        private bool DoesScriptTableExist()
        {
            var sqlText = $"select 1 from information_schema.tables where table_name = '{_configuration.ScriptTable}'";
            object result;

            OpenSqlConnection();

            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;
                result = cmd.ExecuteScalar();
            }

            return null != result;
        }

        private bool EnsureScriptTableExists()
        {
            var exists = DoesScriptTableExist();

            if (!exists)
            {
                LogWarning("Script tracking table did not exist. Creating it now ...");
                CreateScriptTable();
            }
            else
            {
                LogInfo("Script tracking table already exists ...");
            }

            // return true if the table already existed so that the executor knows whether or not to query the table for scripts that have been ran already
            return (exists);
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

        private List<string> GetRanScripts()
        {
            var sqlText = $"select script from {_configuration.ScriptTable} order by Id";
            var ranScripts = new List<string>();

            OpenSqlConnection();

            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ranScripts.Add(reader["Script"] as string);
                    }
                }
            }

            return ranScripts;
        }

        private List<string> LoadSqlScriptFiles(string directory, bool loadAll = false)
        {
            // we want to load scripts that start with <sequence>_all_*.sql or <sequence>_<environment>_*.sql
            const string allRegex = @"[0-9]+_all_.*";
            var envRegex = $"[0-9]+_{_environmentConfiguration.Name}_.*";

            var filesPaths = Directory.GetFiles(directory, "*.sql");

            return loadAll ? filesPaths.Select(Path.GetFileName).ToList() : filesPaths.Select(Path.GetFileName).Where(fn => Regex.IsMatch(fn, allRegex, RegexOptions.IgnoreCase) || Regex.IsMatch(fn, envRegex, RegexOptions.IgnoreCase)).ToList();
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

        private void OpenSqlConnection(bool resettingDatabase = false)
        {
            if (null == _sqlConnection)
            {
                _sqlConnection = new SqlConnection();
            }

            if (_sqlConnection.State == ConnectionState.Closed)
            {
                if (resettingDatabase)
                {
                    LogInfo($"Opening connection to sql server using connection string: {_environmentConfiguration.ResetConnectionString} ...");
                    _sqlConnection.ConnectionString = _environmentConfiguration.ResetConnectionString;
                    _sqlConnection.Open();
                }
                else
                {
                    LogInfo($"Opening connection to sql server using connection string: {_environmentConfiguration.ConnectionString} ...");
                    _sqlConnection.ConnectionString = _environmentConfiguration.ConnectionString;
                    _sqlConnection.Open();
                    _database = _sqlConnection.Database;
                }
            }

            if (_sqlConnection.State == ConnectionState.Open && !resettingDatabase)
            {
                _sqlConnection.ChangeDatabase(_database);
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

        private void Reset()
        {
            // load the reset sql scripts
            LogInfo($"Loading reset script(s) from {_configuration.ResetScriptsFolder} ...");
            var resetScripts = LoadSqlScriptFiles(_configuration.ResetScriptsFolder);
            LogInfo($"Loaded {resetScripts.Count} reset script(s) from {_configuration.ResetScriptsFolder} ...");

            // if there are no scripts to run, just exit
            if (resetScripts.Count == 0)
            {
                LogWarning("No reset script(s) to execute.");
                return;
            }

            // show which scripts were loaded
            foreach (var scriptFile in resetScripts)
            {
                LogInfo($"\t{scriptFile}");
            }

            // run the scripts
            LogWarning("Resetting Database ...");
            foreach (var sqlScriptFileName in resetScripts)
            {
                LogWarning($"\tApplying reset script {sqlScriptFileName} ...");
                RunScriptFile(_configuration.ResetScriptsFolder, sqlScriptFileName, true);
            }

            LogSuccess("Database reset complete.");

            CloseSqlConnection();
        }

        private void RunScriptFile(string folder, string sqlScriptFileName, bool resettingDatabase = false)
        {
            OpenSqlConnection(resettingDatabase);

            var sqlText = File.ReadAllText(Path.Combine(folder, sqlScriptFileName));
            var regex = new Regex(@"\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var lines = regex.Split(sqlText);

            foreach (var line in lines.Where(line => line.Length > 0))
            {
                using (var cmd = _sqlConnection.CreateCommand())
                {
                    cmd.CommandText = line;
                    cmd.ExecuteNonQuery();
                }
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