using System.Reflection;

namespace SqlCi.Cli;

public static class AppVersion
{
    /// <summary>
    /// The current version of the sqlci CLI.
    /// Derived at runtime from the assembly's InformationalVersion attribute
    /// (populated by MSBuild from the -p:Version=... property or Directory.Build.props).
    ///
    /// This design allows fully automated releases driven by git tags:
    /// the CI pipeline sets the version on the publish command and the resulting
    /// binaries report the correct version for --version, update-check, etc.
    /// without any source code changes at release time.
    /// </summary>
    public static string Current
    {
        get
        {
            var attr = typeof(AppVersion).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if (attr is null)
                return "0.0.0";

            // The value is typically "1.2.3+<short-sha>" when built from a git repo.
            // We return only the clean semver part for display and comparison.
            var v = attr.InformationalVersion;
            var plusIndex = v.IndexOf('+');
            return plusIndex >= 0 ? v.Substring(0, plusIndex) : v;
        }
    }
}
