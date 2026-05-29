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
dotnet build src/SqlCi.slnx -c Release

# Run tests (TUnit)
dotnet test src/SqlCi.slnx -c Release

# Run the CLI from source (during development)
dotnet run --project src/SqlCi.Cli -- <command> [args]

# Publish a framework-dependent or self-contained binary
dotnet publish src/SqlCi.Cli -c Release -r win-x64 --self-contained
```

## Cutting a Release (v2.1.0+ Process)

Releases are **fully automated** via Git tags. Pushing an annotated tag with the `v` prefix triggers `.github/workflows/release.yml`, which runs the full test suite, builds the three platform binaries, creates the GitHub Release (using `gh release create --generate-notes`), and attaches the executables + `SHA256SUMS.txt`.

### Pre-Release Checklist (Critical for Quality)

1. **Verify readiness**
   - `master` is up to date and green:
     ```powershell
     dotnet build src/SqlCi.slnx -c Release
     dotnet test src/SqlCi.slnx -c Release
     ```

2. **Update CHANGELOG.md (mandatory before tagging)**
   - Replace the `## [Unreleased]` section with a proper `## [X.Y.Z] - YYYY-MM-DD` entry.
   - Write a concise, human-readable summary at the top (this is the "marketing" text for the release).
   - Use standard Keep a Changelog sections (`### Added`, `### Changed`, `### Fixed`, etc.).
   - Add the release link at the bottom of the file.
   - Commit the CHANGELOG update **before** creating the tag.

3. **Create the annotated tag**
   ```bash
   git tag -a v2.1.0 -m "Release 2.1.0 - short human summary"
   git push origin v2.1.0
   ```

4. **Monitor the workflow**
   - Watch the **Release** workflow in GitHub Actions.
   - The workflow automatically:
     - Runs the full test suite
     - Publishes `win-x64`, `linux-x64`, and `osx-x64` single-file binaries (see `publish-local.ps1` for exact flags)
     - Creates the GitHub Release with `--generate-notes`
     - Attaches versioned binaries + `SHA256SUMS.txt`

5. **Polish the GitHub Release page (for accurate commit logs)**
   - After the workflow completes, visit the new tag page.
   - GitHub's auto-generated "What's Changed" list is often noisy or incomplete.
   - **Edit the release** (pencil icon) and curate it:
     - Put the nice human summary from `CHANGELOG.md` at the top.
     - Clean up or reorganize the commit list so it is accurate and readable.
     - Keep the full raw list only if it adds value.
   - This step is how we guarantee high-quality, accurate release notes.

### Key Principles

- **Two deliverables matter most**:
  1. A good `CHANGELOG.md` entry (human-friendly narrative).
  2. A clean, accurate commit log on the GitHub Release page (achieved via post-creation editing).
- Never edit version numbers in source code. Versioning is driven entirely by the annotated tag via `src/SqlCi.Cli/AppVersion.cs`.
- Clean semver tags (e.g. `v2.1.0`) produce normal releases. Tags containing `-` (e.g. `v2.1.0-beta.1`) are automatically marked as pre-releases.

### References

- Full pipeline: [.github/workflows/release.yml](/github/workflows/release.yml)
- Publish flags & rationale: [publish-local.ps1](/publish-local.ps1) (lines ~67-83)
- Version handling: [src/SqlCi.Cli/AppVersion.cs](/src/SqlCi.Cli/AppVersion.cs)
- Detailed release checklist used by agents: see the release plan in the session notes or the process above.

**When asked to "prepare/generate a release"**, follow the Pre-Release Checklist above exactly, with special emphasis on a high-quality CHANGELOG entry and post-release curation of the GitHub release notes.

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
- The release process changes (update the "Cutting a Release" section and any related checklists)
- CI/CD workflows, `CHANGELOG.md` format expectations, or release-related documentation change

---

**Last reviewed**: See git history for this file.
