# AGENTS.md — SqlCi Development Guide

This document is the authoritative reference for anyone (human or AI agent) working on the SqlCi codebase. **Consult the relevant sections before making changes to CLI, tests, data access, serialization, or build infrastructure.**

## Project Overview

SqlCi is a simple, fast SQL script migration / deployment tool for CI and automated deployments. It supports:

- Environment-specific script execution via `_all_` or `_env_` naming conventions
- Optional reset (drop/create) databases
- Script run history tracking in a version table
- JSON-based configuration (config.json)

The current implementation is a pragmatic modernization to **.NET 10 + C# 14**.

## Technologies & Packages

### .NET 10 and C# 14

- **Purpose in this project**: Primary runtime and language. All projects target `net10.0` with `LangVersion=14`.
- **Key features used**: Nullable reference types (enabled project-wide), file-scoped namespaces, primary constructors, collection expressions, modern pattern matching, implicit usings, global usings.
- **Official documentation**:
  - [.NET 10 documentation](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10)
  - [C# 14 language reference](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14)
  - [Nullable reference types best practices](https://learn.microsoft.com/dotnet/csharp/nullable-references)

### Spectre.Console.Cli

- **Purpose in this project**: Modern command-line parsing and rich UX for the `sqlci` executable (replaces the legacy Mono.Options implementation).
- **Recommended package**: `Spectre.Console.Cli`
- **GitHub**: https://github.com/spectreconsole/spectre.console.cli
- **Official documentation**: https://spectreconsole.net/cli
- **Project notes**:
  - Use `CommandApp`, `Command<TSettings>`, `CommandSettings` with `[CommandArgument]` / `[CommandOption]` attributes.
  - Theming and colored output should align with the historical `StatusLevelEnum` (Info/Success/Warning/Error) behavior.
  - Commands: `init`, `deploy <environment>`, `history <environment>`, `generate <environment> <name>` (or close equivalents).
  - Always update README.md examples when command syntax or help text changes.

### System.Text.Json

- **Purpose in this project**: JSON (de)serialization for `config.json` (replaces Newtonsoft.Json v9).
- **Built-in** — no external package required for basic usage.
- **Official documentation**:
  - [System.Text.Json overview](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json-overview)
  - [How to serialize and deserialize JSON](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json-how-to)
  - [JsonSerializerOptions](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions)
- **Project notes**:
  - Prefer source-generated `JsonSerializerContext` for AOT/trim friendliness where practical.
  - The `Initialize()` output shape must remain compatible with what `LoadAndVerifyConfig` / `Verify` expect.

### Microsoft.Data.SqlClient

- **Purpose in this project**: ADO.NET provider for SQL Server / Azure SQL (replaces the deprecated `System.Data.SqlClient`).
- **Recommended package**: `Microsoft.Data.SqlClient`
- **GitHub**: https://github.com/dotnet/SqlClient
- **NuGet**: https://www.nuget.org/packages/Microsoft.Data.SqlClient/
- **Official documentation**:
  - [Introduction to Microsoft.Data.SqlClient](https://learn.microsoft.com/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace)
  - [API reference](https://learn.microsoft.com/dotnet/api/microsoft.data.sqlclient)
  - [Porting / migration guide from System.Data.SqlClient](https://github.com/dotnet/SqlClient/wiki)
- **Project notes**:
  - Current implementation is SQL Server only (despite older README claims).
  - The simplistic "GO" batch splitter in `Executor.RunScriptFile` is retained for now but is known to be limited.
  - Connection strings and password redaction logic must continue to work.

### TUnit

- **Purpose in this project**: Primary unit / integration test framework (replaced the stale xUnit 2.1 setup).
- **Recommended packages**: `TUnit`, `TUnit.Assertions`
- **GitHub**: https://github.com/thomhurst/TUnit
- **Official documentation**: https://tunit.dev/
- **Key pages**:
  - [Getting started / installation](https://tunit.dev/docs/getting-started/installation)
  - [Writing tests](https://tunit.dev/docs/writing-tests/things-to-know)
  - [Assertions](https://tunit.dev/docs/assertions/getting-started)
  - [Migration from xUnit](https://tunit.dev/docs/migration/xunit)
- **Project notes**:
  - Use `[Test]` (not `[Fact]`), TUnit's fluent `Assert.That(...)` style.
  - Source-generated tests — very fast and AOT friendly.
  - Tests live in `SqlCi.ScriptRunner.Tests` and must be kept in sync with the actual (non-fluent) `Configuration` and `Executor` APIs.

### Directory.Build.props, Directory.Packages.props & Central Package Management (CPM)

- **Purpose in this project**: Consistent build settings, centralized NuGet version management, and reduced duplication across projects.
- **Official Microsoft documentation**:
  - [Customize your build with Directory.Build.props](https://learn.microsoft.com/visualstudio/msbuild/customize-your-build)
  - [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
- **Project notes**:
  - All version pins live in `src/Directory.Packages.props`.
  - Common properties (TargetFramework, LangVersion, Nullable, etc.) live in `src/Directory.Build.props`.
  - Never add `<PackageReference Version="...">` in individual .csproj files unless overriding.

## How to Build, Run & Test

```powershell
# Restore + build everything
dotnet build src/SqlCi.sln -c Release

# Run tests (TUnit)
dotnet test src/SqlCi.sln -c Release

# Run the CLI from source (during development)
dotnet run --project src/SqlCi.Cli -- <command> [args]

# Publish a framework-dependent or self-contained binary
dotnet publish src/SqlCi.Cli -c Release -r win-x64 --self-contained
```

## Cutting a Release (Automated via Git Tags)

Releases are fully automated. When you push a tag, GitHub Actions builds, tests, publishes the three platform binaries, creates the GitHub Release, attaches the executables + SHA256SUMS.txt, and generates release notes.

1. Make sure `master` is green (`dotnet test src/SqlCi.sln -c Release`).
2. Create an annotated tag (use `v` prefix):
   ```bash
   git tag -a v1.2.3 -m "Release 1.2.3 - one-line summary of notable changes"
   git push origin v1.2.3
   ```
3. Watch the Actions run (`Release` workflow). It will:
   - Run the full test suite.
   - Publish clean single-file self-contained executables for `win-x64`, `linux-x64`, and `osx-x64` (exact same settings as `publish-local.ps1`, including `IncludeNativeLibrariesForSelfExtract` and no AOT/trimming).
   - Create a GitHub Release for the tag, attach versioned binaries (`sqlci-1.2.3-win-x64.exe` etc.) + checksums, and populate the release body with `--generate-notes` (a "What's Changed" list of commits/PRs).
   - Prerelease tags (containing `-`, e.g. `v1.2.0-beta.1`) are automatically marked as prereleases.
4. Once complete, the new version is immediately visible to `sqlci update-check` worldwide.

See:
- `.github/workflows/release.yml` for the full pipeline.
- `publish-local.ps1` (lines 67-83 and cleanup logic) for the authoritative publish arguments and rationale.
- `src/SqlCi.Cli/AppVersion.cs` (the dynamic version property that makes tag-driven releases work without editing source).

**Tip**: For human-readable highlights on important releases, add a short entry to the top of `CHANGELOG.md` before tagging. The automated commit list from `--generate-notes` provides the detailed "what changed" view on the Releases page.

## Important Architectural Notes

- The `Executor` class still contains the core deployment logic (script loading, GO splitting, audit table, reset flow).
- `Configuration` + `EnvironmentConfiguration` + `Verify()` are the source of truth for validation (tests were aligned to this simpler model).
- Password redaction happens in `Executor.RedactSecrets` before any status events are raised.
- Script naming convention (`<sequence>_<all|env>_*.sql`) is business logic and must not be changed without updating README and tests.

## When to Update This File

Update `AGENTS.md` whenever:
- A major package or technology is added, removed, or replaced
- Significant architectural decisions are made (record links to ADRs if used)
- New mandatory patterns emerge (e.g., "always use primary constructors for DTOs")
- CI/CD workflows, release process, or release-related documentation (`release.yml`, `CHANGELOG.md`, etc.) change

---

**Last reviewed**: See git history for this file.
