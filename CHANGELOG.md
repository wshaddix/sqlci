# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.1.1] - 2026-05-29

### Security
- Validate `ScriptTable` as a safe SQL identifier (reject names with spaces, semicolons, etc.) before interpolation into DDL/DML across all providers. Invalid names now produce a clear error at config validation time rather than failing at the database level.
- Harden connection-string secret redaction: the `Pwd` keyword is now recognized, arbitrary whitespace around `=` is handled, and password values without a trailing semicolon are redacted.

### Fixed
- Each change script and its tracking-table audit row now execute inside a **single transaction**. If the script fails, its audit is rolled back — previously a crash between script execution and audit insertion would cause the script to re-run on the next deploy.
- Configuration validation now runs the full suite of checks (ScriptTable, Version, ScriptsFolder existence) at deploy time rather than only validating the target environment. Previously, a missing scripts folder would surface as a cryptic `DirectoryNotFoundException` mid-deploy.
- `ExtractIdFromFileName` no longer crashes on filenames without an underscore; it now throws a clear message describing the required naming convention.
- SQL Server `GO` batch separator is now anchored to the start of the line (`^\s*GO\s*$`) so words ending in "GO" (e.g. `CARGO`) are not accidentally split.
- Generated script timestamps now use UTC instead of local time, producing monotonic, timezone-independent sequence prefixes.
- Script run history (`sqlci history`) no longer creates the tracking table as a side effect — it only reads.

### Changed
- `Executor` now implements `IDisposable` and holds a single connection per phase (deploy / reset / history) rather than opening and closing one per operation.
- Provider alias handling (SqlServer/MSSQL, PostgreSql/pgsql, etc.) is centralized in `DatabaseProviderFactory.Normalize`, and `sqlci init` now writes the canonical provider name into config.
- Unused `ExceptionMessages` removed and remaining constants converted to `const`. Default assembly version set to `0.0.0-dev` so un-versioned builds are obvious.

### Removed
- Dead code: unused `_database` field in `Executor`, unused `LogError` method, unused `MissingResetFolder` / `NotVerified` / `ResetFolderDoesNotExist` exception messages.

[2.1.1]: https://github.com/wshaddix/sqlci/releases/tag/v2.1.1

## [2.1.0] - 2026-06-XX

### Added
- Interactive database provider selection when running `sqlci init`. When the `--provider` flag is omitted in an interactive terminal, SqlCi now shows a `SelectionPrompt` allowing users to choose between `Sqlite`, `SqlServer`, and `PostgreSql`.
- New `CONTRIBUTING.md` with development setup, build/test commands, and contribution guidelines.

### Changed
- **Major documentation overhaul**: README completely rewritten with improved flow (Features → Installation with curl/wget instructions → Getting Started happy path → detailed reference sections later). Significantly better quick-start experience.
- `sqlci init` now defaults to **Sqlite** with a working `Data Source=local.db;Cache=Shared` connection string (instead of a SQL Server LocalDB placeholder). Generated connection strings are now appropriate for the selected provider.
- Legacy internal documentation in `docs/` was removed.

### Removed
- The entire legacy `src/SqlCi` project and old `.sln` solution file (part of ongoing modernization cleanup).

### Fixed
- `update-check --prerelease` flag behavior.

[2.1.0]: https://github.com/wshaddix/sqlci/releases/tag/v2.1.0

## [2.0.0-beta.1] - 2026-06-XX

### Added
- Automated GitHub Actions release pipeline: tagging the repo now builds self-contained executables for Windows, Linux, and macOS and publishes them to GitHub Releases.
- Dynamic version reporting (`AppVersion.Current`) driven from MSBuild at publish time — no more manual sync between source and release tags.
- `CHANGELOG.md` following Keep a Changelog format.
- CI workflow for continuous build + test on push/PR.

### Changed
- **Major version bump** to 2.0.0 series as part of the full modernization (new Spectre.Console.Cli architecture, .NET 10, provider abstraction, TUnit tests, etc.).
- This is a **pre-release** (beta.1) to validate the new automated release process end-to-end.

### Removed
- Legacy `SqlCi.Console` project and old Mono.Options-based CLI (fully replaced by `SqlCi.Cli` + Spectre.Console.Cli).

## [1.0.0] - 2026-05-28

### Added
- Initial public release of the modernized SqlCi (Spectre.Console.Cli, .NET 10, cross-platform self-contained binaries).
- Support for SqlServer, PostgreSql, and Sqlite via pluggable providers.
- `update-check` command that talks to GitHub Releases.
- Full rewrite of the CLI command structure while preserving script naming conventions and deployment semantics.

[1.0.0]: https://github.com/wshaddix/sqlci/releases/tag/v1.0.0
