using SqlCi.ScriptRunner;
using SqlCi.ScriptRunner.Providers;

namespace SqlCi.Cli;

public static class DatabaseProviderResolver
{
    public static IDatabaseProvider ResolveForEnvironment(EnvironmentConfiguration env)
    {
        return DatabaseProviderFactory.Create(env.DbProvider);
    }
}
