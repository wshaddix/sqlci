using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using SqlCi.ScriptRunner.Constants;
using SqlCi.ScriptRunner.Events;
using SqlCi.ScriptRunner.Exceptions;

namespace SqlCi.ScriptRunner
{
    public class Executor
    {
        private ScriptConfiguration _scriptConfiguration;
        private SqlConnection _sqlConnection;
        private string _database;

        public event EventHandler<StatusUpdateEvent> StatusUpdate;

        public ExecutionResults Execute(ScriptConfiguration scriptConfiguration)
        {
            OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Verifying configuration ..."));

            if (scriptConfiguration == null) throw new ArgumentNullException("scriptConfiguration");

            // make sure that Verify() was called before we try to do any work
            if (!scriptConfiguration.IsVerified)
            {
                throw new NotVerifiedException(ExceptionMessages.NotVerified);
            }

            OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Configuration verification complete. Starting deployment ..."));

            // store the config so other instance methods can reference it
            _scriptConfiguration = scriptConfiguration;

            // load the sql scripts
            OnRaiseStatusUpdateEvent(new StatusUpdateEvent(string.Format("Loading change script(s) from {0} ...", _scriptConfiguration.ScriptsFolder)));
            var allScriptFiles = LoadSqlScriptFiles();
            OnRaiseStatusUpdateEvent(new StatusUpdateEvent(string.Format("Loaded {0} change script(s) from {1} ...", allScriptFiles.Count, _scriptConfiguration.ScriptsFolder)));

            // if there are no scripts to run, just exit
            if (allScriptFiles.Count == 0)
            {
                OnRaiseStatusUpdateEvent(new StatusUpdateEvent("No change script(s) to execute. Deployment Complete."));
                return new ExecutionResults(true);
            }

            try
            {
                OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Checking for existance of script tracking table in the database ..."));
                // ensure that the script table exists
                var tableExisted = EnsureScriptTableExists();

                List<string> sqlScriptFiles;
                if (tableExisted)
                {
                    // find out which scripts have already been ran 
                    OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Checking to see which change script(s) have already been applied ..."));
                    var ranScriptFiles = GetRanScripts();
                    OnRaiseStatusUpdateEvent(new StatusUpdateEvent(string.Format("Found {0} change script(s) that have already been applied ...", ranScriptFiles.Count())));

                    // remove any scripts that have already ran 
                    OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Calculating which new change script(s) need to be applied ..."));
                    sqlScriptFiles = allScriptFiles.Except(ranScriptFiles).ToList();
                    OnRaiseStatusUpdateEvent(new StatusUpdateEvent(string.Format("{0} new change script(s) need to be applied ...", sqlScriptFiles.Count)));
                }
                else
                {
                    // if the script table didn't already exist then we need to run
                    // all of the sql scripts
                    sqlScriptFiles = allScriptFiles;
                    OnRaiseStatusUpdateEvent(new StatusUpdateEvent(string.Format("{0} new script(s) need to be applied ...", sqlScriptFiles.Count)));
                }

                // run the scripts
                foreach (var sqlScriptFileName in sqlScriptFiles)
                {
                    OnRaiseStatusUpdateEvent(new StatusUpdateEvent(string.Format("Applying change script {0} ...", sqlScriptFileName)));
                    RunScriptFile(sqlScriptFileName);
                    AuditScriptRan(sqlScriptFileName);
                }

                OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Deployment complete."));
                return new ExecutionResults(true);
            }
            finally
            {
                CloseSqlConnection();
            }
        }

        private void CloseSqlConnection()
        {
            if (_sqlConnection.State != ConnectionState.Closed)
            {
                OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Closing connection to sql server ..."));
                _sqlConnection.Close();
            }
        }

        private void AuditScriptRan(string sqlScriptFileName)
        {
            OpenSqlConnection();

            var id = ExtractIdFromFileName(sqlScriptFileName);

            var sqlText = string.Format("insert into {0} (Id, Script, Release, AppliedOnUtc) values " +
                                        "('{1}', '{2}', '{3}', '{4}')", 
                                        _scriptConfiguration.ScriptTable,
                                        id, sqlScriptFileName, _scriptConfiguration.ReleaseNumber, DateTime.UtcNow);
            
            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;
                cmd.ExecuteNonQuery();
            }
        }

        private string ExtractIdFromFileName(string sqlScriptFileName)
        {
            // get just the file name and not the full path
            var fileName = Path.GetFileName(sqlScriptFileName);

            // take anything left of the first underscore
            var underscoreIndex = fileName.IndexOf('_');
            return fileName.Substring(0, underscoreIndex);
        }

        private void RunScriptFile(string sqlScriptFileName)
        {
            OpenSqlConnection();

            var sqlText = File.ReadAllText(Path.Combine(_scriptConfiguration.ScriptsFolder, sqlScriptFileName));
            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;
                cmd.ExecuteNonQuery();
            }
        }

        private List<string> GetRanScripts()
        {
            var sqlText = string.Format("select script from {0} order by Id", _scriptConfiguration.ScriptTable);
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

        private List<string> LoadSqlScriptFiles()
        {            
            var filesPaths = Directory.GetFiles(_scriptConfiguration.ScriptsFolder, "*.sql").Where(fp => !fp.ToLowerInvariant().EndsWith("_rollback.sql"));
            return filesPaths.Select(fp => Path.GetFileName(fp)).ToList();
        }

        private bool EnsureScriptTableExists()
        {
            var sqlText = string.Format("select 1 from information_schema.tables where table_name = '{0}'", _scriptConfiguration.ScriptTable);
            object result;

            OpenSqlConnection();

            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;
                result = cmd.ExecuteScalar();
            }

            if (null == result)
            {
                OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Script tracking table did not exist. Creating it now ..."));
                CreateScriptTable();
            }
            else
            {
                OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Script tracking table already exists ..."));
            }

            // return true if the table already existed so that the executor
            // knows whether or not to query the table for scripts that have
            // been ran already
            return (null != result);
        }

        private void CreateScriptTable()
        {
            var sqlText = string.Format("create table {0} (Id nvarchar(5) not null constraint pk primary key clustered, Script nvarchar(255) not null, Release nvarchar(25) not null, AppliedOnUtc datetime not null)", _scriptConfiguration.ScriptTable);
            OpenSqlConnection();
            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;
                cmd.ExecuteNonQuery();
            }
            OnRaiseStatusUpdateEvent(new StatusUpdateEvent("Script tracking table was created ..."));
        }

        private void OpenSqlConnection()
        {
            if (null == _sqlConnection)
            {
                _sqlConnection = new SqlConnection();
            }
            
            if (_sqlConnection.State == ConnectionState.Closed)
            {
                OnRaiseStatusUpdateEvent(new StatusUpdateEvent(string.Format("Opening connection to sql server using connection string: {0} ...", _scriptConfiguration.ConnectionString)));
                _sqlConnection.ConnectionString = _scriptConfiguration.ConnectionString;
                _sqlConnection.Open();
                _database = _sqlConnection.Database;
            }

            if (_sqlConnection.State == ConnectionState.Open)
            {
                _sqlConnection.ChangeDatabase(_database);
            }
        }

        // Wrap event invocations inside a protected virtual method 
        // to allow derived classes to override the event invocation behavior 
        protected virtual void OnRaiseStatusUpdateEvent(StatusUpdateEvent e)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            EventHandler<StatusUpdateEvent> handler = StatusUpdate;

            // Event will be null if there are no subscribers 
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }
    }
}