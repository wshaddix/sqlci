namespace SqlCi.ScriptRunner.Exceptions;

public class ConfigurationException(string? message = null, Exception? inner = null) : Exception(message, inner)
{
}