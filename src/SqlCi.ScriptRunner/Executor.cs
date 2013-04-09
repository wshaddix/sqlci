using System.Text.RegularExpressions;
using SqlCi.ScriptRunner.Constants;
using SqlCi.ScriptRunner.Events;
using SqlCi.ScriptRunner.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

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
            // let our host know we are about to verify the config
            UpdateStatus("Verifying configuration ...");

            // no config means we can't proceed
            if (scriptConfiguration == null) throw new ArgumentNullException("scriptConfiguration");

            // make sure that the host called Verify() on the scriptConfiguration before we try to do any work
            if (!scriptConfiguration.IsVerified)
            {
                throw new NotVerifiedException(ExceptionMessages.NotVerified);
            }

            // let our host know configuration has been verified
            UpdateStatus("Configuration verification complete.");

            // store the config so other instance methods can reference it later on
            _scriptConfiguration = scriptConfiguration;

            // if we need to reset the database do that first
            if (_scriptConfiguration.ResetDatabase)
            {
                UpdateStatus("Resetting the database ...");
                Reset();
            }

            // deploy
            UpdateStatus("Deploying version {0} to {1}", scriptConfiguration.ReleaseNumber, _scriptConfiguration.Environment);
            var results = Deploy();

            UpdateStatus("Deployment Complete.");

            // return the results to our host
            return results;
        }

        private void Reset()
        {
            // load the reset sql scripts
            UpdateStatus("Loading reset script(s) from {0} ...", _scriptConfiguration.ResetFolder);
            var resetScripts = LoadSqlScriptFiles(_scriptConfiguration.ResetFolder, true);
            UpdateStatus("Loaded {0} reset script(s) from {1} ...", resetScripts.Count, _scriptConfiguration.ResetFolder);

            // if there are no scripts to run, just exit
            if (resetScripts.Count == 0)
            {
                UpdateStatus("No reset script(s) to execute.");
                return;
            }

            // show which scripts were loaded
            foreach (var scriptFile in resetScripts)
            {
                UpdateStatus("\t{0}", scriptFile);
            }

            // run the scripts
            UpdateStatus("Resetting Database ...");
            foreach (var sqlScriptFileName in resetScripts)
            {
                UpdateStatus("\tApplying reset script {0} ...", sqlScriptFileName);
                RunScriptFile(_scriptConfiguration.ResetFolder, sqlScriptFileName, true);
            }

            UpdateStatus("Database reset complete.");
        }
        
        private ExecutionResults Deploy()
        {
            // load the sql scripts
            UpdateStatus("Loading change script(s) from {0} ...", _scriptConfiguration.ScriptsFolder);
            var allScriptFiles = LoadSqlScriptFiles(_scriptConfiguration.ScriptsFolder);
            UpdateStatus("Loaded {0} change script(s) from {1} ...", allScriptFiles.Count, _scriptConfiguration.ScriptsFolder);

            // if there are no scripts to run, just exit
            if (allScriptFiles.Count == 0)
            {
                UpdateStatus("No change script(s) to execute.");
                return new ExecutionResults(true);
            }
            
            // show which scripts were loaded
            foreach (var scriptFile in allScriptFiles)
            {
                UpdateStatus("\t{0}", scriptFile);
            }

            try
            {
                UpdateStatus("Checking for existance of script tracking table in the database ...");
                
                // ensure that the script table exists
                var tableExisted = EnsureScriptTableExists();

                List<string> sqlScriptFiles;
                if (tableExisted)
                {
                    // find out which scripts have already been ran 
                    UpdateStatus("Checking to see which change script(s) have already been applied ...");
                    var ranScriptFiles = GetRanScripts();
                    UpdateStatus("Found {0} change script(s) that have already been applied ...", ranScriptFiles.Count());

                    // show which scripts were applied
                    foreach (var scriptFile in ranScriptFiles)
                    {
                        UpdateStatus("\t{0}", scriptFile);
                    }

                    // remove any scripts that have already ran 
                    UpdateStatus("Calculating which new change script(s) need to be applied ...");
                    sqlScriptFiles = allScriptFiles.Except(ranScriptFiles).ToList();
                    UpdateStatus("{0} new change script(s) need to be applied ...", sqlScriptFiles.Count);
                }
                else
                {
                    // if the script table didn't already exist then we need to run
                    // all of the sql scripts
                    sqlScriptFiles = allScriptFiles;
                    UpdateStatus("{0} new script(s) need to be applied ...", sqlScriptFiles.Count);
                }

                // run the scripts
                foreach (var sqlScriptFileName in sqlScriptFiles)
                {
                    UpdateStatus("\tApplying change script {0} ...", sqlScriptFileName);
                    RunScriptFile(_scriptConfiguration.ScriptsFolder, sqlScriptFileName);
                    AuditScriptRan(sqlScriptFileName);
                }

                UpdateStatus("Deployment complete.");
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
                UpdateStatus("Closing connection to sql server ...");
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

        private void RunScriptFile(string folder, string sqlScriptFileName, bool resettingDatabase=false)
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

        private List<string> LoadSqlScriptFiles(string directory, bool loadAll=false)
        {
            // we want to load scripts that start with <sequence>_all_*.sql or <sequence>_<environment>_*.sql
            const string allRegex = @"[0-9]+_all_.*";
            var envRegex = string.Format("[0-9]+_{0}_.*", _scriptConfiguration.Environment);

            var filesPaths = Directory.GetFiles(directory, "*.sql");

            return loadAll ? filesPaths.Select(Path.GetFileName).ToList() : filesPaths.Select(Path.GetFileName).Where(fn => Regex.IsMatch(fn, allRegex,RegexOptions.IgnoreCase) || Regex.IsMatch(fn, envRegex, RegexOptions.IgnoreCase)).ToList();
        }

        private bool EnsureScriptTableExists()
        {
            var exists = DoesScriptTableExist();

            if(!exists)
            {
                UpdateStatus("Script tracking table did not exist. Creating it now ...");
                CreateScriptTable();
            }
            else
            {
                UpdateStatus("Script tracking table already exists ...");
            }

            // return true if the table already existed so that the executor
            // knows whether or not to query the table for scripts that have
            // been ran already
            return (exists);
        }

        private bool DoesScriptTableExist()
        {
            var sqlText = string.Format("select 1 from information_schema.tables where table_name = '{0}'", _scriptConfiguration.ScriptTable);
            object result;

            OpenSqlConnection();

            using (var cmd = _sqlConnection.CreateCommand())
            {
                cmd.CommandText = sqlText;
                result = cmd.ExecuteScalar();
            }

            return null != result;
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
            UpdateStatus("Script tracking table was created ...");
        }

        private void OpenSqlConnection(bool resettingDatabase = false)
        {
            if (null == _sqlConnection)
            {
                _sqlConnection = new SqlConnection();
            }
            
            if (_sqlConnection.State == ConnectionState.Closed)
            {
                UpdateStatus("Opening connection to sql server using connection string: {0} ...", _scriptConfiguration.ConnectionString);
                _sqlConnection.ConnectionString = _scriptConfiguration.ConnectionString;
                _sqlConnection.Open();
                _database = _sqlConnection.Database;
            }

            if (_sqlConnection.State == ConnectionState.Open && !resettingDatabase)
            {
                _sqlConnection.ChangeDatabase(_database);
            }
        }

        private void UpdateStatus(string format, params Object[] args)
        {
            if (null == args || args.Length == 0)
            {
                OnRaiseStatusUpdateEvent(new StatusUpdateEvent(format));
            }
            else
            {
                OnRaiseStatusUpdateEvent(new StatusUpdateEvent(string.Format(format, args)));
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