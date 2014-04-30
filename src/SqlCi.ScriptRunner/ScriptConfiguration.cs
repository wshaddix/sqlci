using SqlCi.ScriptRunner.Constants;
using SqlCi.ScriptRunner.Exceptions;
using System.IO;

namespace SqlCi.ScriptRunner
{
    public class ScriptConfiguration
    {
        private string _connectionString;
        private string _environment;
        private string _releaseNumber;
        private string _resetConnectionString;
        private bool _resetDatabase;
        private string _resetFolder;
        private string _scriptsFolder;
        private string _scriptTable;
        private bool _verified;
        public string ConnectionString
        {
            get { return _connectionString; }
        }
        public string Environment
        {
            get { return _environment; }
        }
        public bool IsVerified
        {
            get { return _verified; }
        }
        public string ReleaseNumber
        {
            get { return _releaseNumber; }
        }
        public string ResetConnectionString
        {
            get { return _resetConnectionString; }
        }
        public bool ResetDatabase
        {
            get { return _resetDatabase; }
        }
        public string ResetFolder
        {
            get { return _resetFolder; }
        }
        public string ScriptsFolder
        {
            get { return _scriptsFolder; }
        }
        public string ScriptTable
        {
            get { return _scriptTable; }
        }

        public ScriptConfiguration Verify()
        {
            // do a sanity check on our variables and make sure we have everything we need to run
            // the scripts
            ValidateConnectionString();
            ValidateScriptsFolder();
            ValidateResetFolder();
            ValidateReleaseNumber();
            ValidateScriptTable();
            ValidateEnvironment();
            ValidateResetConnectionString();

            // if we got this far without errors then we are ready to run scripts
            _verified = true;

            return this;
        }

        public ScriptConfiguration WithConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public ScriptConfiguration WithEnvironment(string environment)
        {
            _environment = environment;
            return this;
        }

        public ScriptConfiguration WithReleaseNumber(string releaseNumber)
        {
            _releaseNumber = releaseNumber;
            return this;
        }

        public ScriptConfiguration WithResetConnectionString(string resetConnectionString)
        {
            _resetConnectionString = resetConnectionString;
            return this;
        }

        public ScriptConfiguration WithResetDatabase(bool resetDatabase)
        {
            _resetDatabase = resetDatabase;
            return this;
        }

        public ScriptConfiguration WithResetFolder(string resetFolder)
        {
            _resetFolder = resetFolder;
            return this;
        }

        public ScriptConfiguration WithScriptsFolder(string scriptsFolder)
        {
            _scriptsFolder = scriptsFolder;
            return this;
        }

        public ScriptConfiguration WithScriptTable(string scriptVersionTable)
        {
            _scriptTable = scriptVersionTable;
            return this;
        }

        private void ValidateConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new MissingConnectionStringException(ExceptionMessages.MissingConnectionString);
            }
        }

        private void ValidateEnvironment()
        {
            if (string.IsNullOrEmpty(_environment))
            {
                throw new MissingEnvironmentException(ExceptionMessages.MissingEnvironment);
            }
        }

        private void ValidateReleaseNumber()
        {
            if (string.IsNullOrEmpty(_releaseNumber))
            {
                throw new MissingReleaseNumberException(ExceptionMessages.MissingReleaseNumber);
            }
        }

        private void ValidateResetConnectionString()
        {
            if (_resetDatabase && string.IsNullOrEmpty(_resetConnectionString))
            {
                throw new MissingConnectionStringException(ExceptionMessages.MissingResetConnectionString);
            }
        }

        private void ValidateResetFolder()
        {
            if (_resetDatabase && string.IsNullOrEmpty(_resetFolder))
            {
                throw new MissingResetFolderException(ExceptionMessages.MissingResetFolder);
            }

            if (_resetDatabase && !Directory.Exists(_resetFolder))
            {
                throw new ResetFolderDoesNotExistException(ExceptionMessages.ResetFolderDoesNotExist);
            }
        }

        private void ValidateScriptsFolder()
        {
            if (string.IsNullOrEmpty(_scriptsFolder))
            {
                throw new MissingScriptsFolderException(ExceptionMessages.MissingScriptsFolder);
            }

            if (!Directory.Exists(_scriptsFolder))
            {
                throw new ScriptsFolderDoesNotExistException(ExceptionMessages.ScriptsFolderDoesNotExist);
            }
        }

        private void ValidateScriptTable()
        {
            if (string.IsNullOrEmpty(_scriptTable))
            {
                throw new MissingScriptTableException(ExceptionMessages.MissingScriptTable);
            }
        }
    }
}