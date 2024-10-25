using SqlCi.ScriptRunner.Constants;
using SqlCi.ScriptRunner.Exceptions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SqlCi.ScriptRunner
{
    public class Configuration
    {
        public List<EnvironmentConfiguration> Environments { get; set; }
        public string ResetScriptsFolder { get; set; }
        public string ScriptsFolder { get; set; }
        public string ScriptTable { get; set; }
        public string Version { get; set; }

        public Configuration()
        {
            Environments = new List<EnvironmentConfiguration>();
        }

        public void Verify()
        {
            var sb = new StringBuilder();

            // script table
            if (string.IsNullOrEmpty(Version))
            {
                sb.AppendLine(ExceptionMessages.MissingScriptTable);
            }

            // version
            if (string.IsNullOrEmpty(Version))
            {
                sb.AppendLine(ExceptionMessages.MissingReleaseNumber);
            }

            // scripts folder
            if (string.IsNullOrEmpty(ScriptsFolder))
            {
                sb.AppendLine(ExceptionMessages.MissingScriptsFolder);
            }

            if (!Directory.Exists(ScriptsFolder))
            {
                sb.AppendLine(ExceptionMessages.ScriptsFolderDoesNotExist);
            }

            // if we have any errors throw an exception
            if (sb.Length > 0)
            {
                throw new ConfigurationException(sb.ToString());
            }
        }

        public EnvironmentConfiguration Verify(string environment)
        {
            if (string.IsNullOrWhiteSpace(environment))
            {
                throw new ConfigurationException(ExceptionMessages.MissingEnvironment);
            }

            var sb = new StringBuilder();

            var environmentConfig = Environments.FirstOrDefault(e => e.Name.ToUpper().Equals(environment.ToUpper()));

            if (null == environmentConfig)
            {
                throw new ConfigurationException($"The environment {environment} does not exist in config.json");
            }

            if (string.IsNullOrEmpty(environmentConfig.ConnectionString))
            {
                sb.AppendLine(ExceptionMessages.MissingConnectionString);
            }

            if (environmentConfig.ResetDatabase && string.IsNullOrWhiteSpace(environmentConfig.ResetConnectionString))
            {
                sb.AppendLine(ExceptionMessages.MissingResetConnectionString);
            }

            // if we have any errors throw an exception
            if (sb.Length > 0)
            {
                throw new ConfigurationException(sb.ToString());
            }

            return environmentConfig;
        }
    }
}