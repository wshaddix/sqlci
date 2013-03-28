
using System;
using System.Collections.Generic;
using System.IO;
using SqlCi.ScriptRunner.Constants;
using SqlCi.ScriptRunner.Exceptions;

namespace SqlCi.ScriptRunner
{
    public class ScriptConfiguration
    {
        private string _connectionString;
        private string _scriptsFolder;
        private string _resetFolder;
        private bool _logToConsole;
        private string _releaseNumber;
        private string _scriptTable;
        private bool _resetDatabase;
        private bool _verified;
        private List<string> _errors;

        public string ScriptsFolder
        {
            get { return _scriptsFolder; }
        }
        
        public string ConnectionString
        {
            get { return _connectionString; }
        }

        public string ScriptTable
        {
            get { return _scriptTable; }
        }

        public string ReleaseNumber
        {
            get { return _releaseNumber; }
        }

        public ScriptConfiguration()
        {
            _errors = new List<string>();
        }
        
        public bool IsVerified
        {
            get { return _verified; }
        }

        public ScriptConfiguration WithConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public ScriptConfiguration WithScriptsFolder(string scriptsFolder)
        {
            _scriptsFolder = scriptsFolder;
            return this;
        }

        public ScriptConfiguration ResetDatabase(bool resetDatabase)
        {
            _resetDatabase = resetDatabase;
            return this;
        }

        public ScriptConfiguration ResetFolder(string resetFolder)
        {
            _resetFolder = resetFolder;
            return this;
        }

        public ScriptConfiguration LogToConsole(bool logToConsole)
        {
            _logToConsole = logToConsole;
            return this;
        }

        public ScriptConfiguration WithReleaseNumber(string releaseNumber)
        {
            _releaseNumber = releaseNumber;
            return this;
        }

        public ScriptConfiguration WithScriptTable(string scriptVersionTable)
        {
            _scriptTable = scriptVersionTable;
            return this;
        }

        public ScriptConfiguration Verify()
        {
            // do a sanity check on our variables and make sure we have
            // everything we need to run the scripts
            ValidateConnectionString();
            ValidateScriptsFolder();
            ValidateResetFolder();
            ValidateReleaseNumber();
            ValidateScriptTable();
            
            // if we got this far without errors then we are ready to run scripts
            if (_errors.Count == 0)
            {
                _verified = true;
            }
            
            return this;
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

        private void ValidateConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new MissingConnectionStringException(ExceptionMessages.MissingConnectionString);
            }
        }

        private void ValidateReleaseNumber()
        {
            if (string.IsNullOrEmpty(_releaseNumber))
            {
                throw new MissingReleaseNumberException(ExceptionMessages.MissingReleaseNumber);
            }
        }

        private void ValidateScriptTable()
        {
            if (string.IsNullOrEmpty(_scriptTable))
            {
                throw new MissingScriptTableException(ExceptionMessages.MissingScriptTable);
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
    }
}
