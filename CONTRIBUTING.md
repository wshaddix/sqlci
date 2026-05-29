# Contributing to SqlCi

Thanks for your interest in improving SqlCi!

## Development Setup

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A code editor (Visual Studio, VS Code + C# Dev Kit, or Rider)
- (Optional but recommended for integration tests) Docker Desktop for spinning up real SQL Server and PostgreSQL containers

## Build & Test

```powershell
# Restore + build the entire solution
dotnet build src/SqlCi.slnx -c Release

# Run the full test suite (TUnit)
dotnet test src/SqlCi.slnx -c Release
```

## Running the CLI During Development

```powershell
# Any command works — examples:
dotnet run --project src/SqlCi.Cli -- --help
dotnet run --project src/SqlCi.Cli -- init
dotnet run --project src/SqlCi.Cli -- generate local add_users_table
dotnet run --project src/SqlCi.Cli -- deploy local
dotnet run --project src/SqlCi.Cli -- history local
dotnet run --project src/SqlCi.Cli -- update-check
```

The working directory matters — `init`, `generate`, etc. create files relative to the current directory.

## Publishing Local Binaries

See [publish-local.ps1](/publish-local.ps1) for the exact flags used to produce the official single-file, self-contained releases (no AOT/trimming, native libs extracted, symbols stripped).

Typical one-off publish:

```powershell
dotnet publish src/SqlCi.Cli -c Release -r win-x64 --self-contained -o ./artifacts/sqlci-win-x64 `
  -p:PublishSingleFile=true -p:PublishAot=false -p:PublishTrimmed=false `
  -p:IncludeNativeLibrariesForSelfExtract=true -p:InvariantGlobalization=true -p:StripSymbols=true
```

## Releases & Versioning

Releases are **fully automated**. Push an annotated tag and GitHub Actions does the rest:

```bash
git tag -a v2.1.0 -m "Release 2.1.0 - short summary"
git push origin v2.1.0
```

The pipeline (see [.github/workflows/release.yml](/github/workflows/release.yml)):
- Builds clean single-file executables for `win-x64`, `linux-x64`, and `osx-x64`
- Attaches versioned binaries + `SHA256SUMS.txt` to a GitHub Release
- Generates release notes via `--generate-notes`
- Marks prereleases automatically when the tag contains `-` (e.g. `v2.0.0-beta.1`)

Version numbers come from the tag at publish time (via MSBuild `InformationalVersion`). See [src/SqlCi.Cli/AppVersion.cs](/src/SqlCi.Cli/AppVersion.cs) for how the CLI reports its own version.

## Full Guidelines (Architecture, Testing, Agents)

See [AGENTS.md](/AGENTS.md) — it is the authoritative reference for:
- Technology choices and why (Spectre.Console.Cli, TUnit, System.Text.Json, Microsoft.Data.SqlClient, etc.)
- Coding conventions and C# 14 / .NET 10 patterns
- How the Executor, Configuration, and providers work
- Test expectations and the (intentionally simple) script batching behavior
- Release process details

All humans and AI agents working on the codebase should consult AGENTS.md before making changes.

## Code of Conduct / Contribution Workflow

- Open an issue or discussion before large changes.
- Keep PRs focused (one logical change per PR).
- Update README.md examples whenever command syntax, help text, or default behavior changes.
- New features that affect script execution or configuration should include updates to tests in `SqlCi.ScriptRunner.Tests`.

Happy shipping!
