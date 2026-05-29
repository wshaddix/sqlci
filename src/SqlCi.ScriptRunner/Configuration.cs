using SqlCi.ScriptRunner.Constants;
using SqlCi.ScriptRunner.Exceptions;
using SqlCi.ScriptRunner.Providers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SqlCi.ScriptRunner;

public class Configuration
{
    public List<EnvironmentConfiguration> Environments { get; set; } = new();
    public required string ResetScriptsFolder { get; set; }
    public required string ScriptsFolder { get; set; }
    public required string ScriptTable { get; set; }
    public required string Version { get; set; }

    public void Verify()
        {
            var sb = new StringBuilder();

            // script table
            if (string.IsNullOrEmpty(ScriptTable))
            {
                sb.AppendLine(ExceptionMessages.MissingScriptTable);
            }
            else if (!ProviderHelpers.IsValidIdentifier(ScriptTable))
            {
                sb.AppendLine(ExceptionMessages.InvalidScriptTable);
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
            else if (!Directory.Exists(ScriptsFolder))
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
            // Validate the global configuration (script table, version, scripts folder) up front
            // so that callers always get a complete, early failure rather than an obscure error later.
            Verify();

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