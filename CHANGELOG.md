# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
