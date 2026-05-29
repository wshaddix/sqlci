# SqlCi

A fast, simple SQL migration tool for teams and CI/CD pipelines.

Deploy schema changes, reference data, and environment-specific scripts to **SQL Server**, **PostgreSQL**, or **SQLite** with a single command and a JSON config.

## Features

- **Single-file, cross-platform binaries** — download one executable for Windows, Linux, or macOS. No runtime or installer required.
- **Three database providers** — Sqlite (default at `init`), PostgreSQL, and SQL Server. Choose interactively, via `--provider`, or per environment in `config.json`.
- **Environment-aware scripts** — name scripts with `_all_` (runs everywhere) or `_local_`, `_qa_`, `_prod_`, etc. (runs only for that environment).
- **Idempotent deployments** — tracks every applied script in a simple audit table. Re-running the same deployment is safe.
- **Optional reset (drop/recreate)** — per-environment flag that runs scripts from a separate `ResetScripts` folder before the normal migration. Perfect for dev workstations and test environments.
- **Secret hygiene built-in** — reference environment variables with `${VAR_NAME}` or override entire connection strings via `SQLCI_PROD_CONNECTION` for CI/CD secret managers.
- **CI/CD friendly** — clean exit codes (0 = success, -1 = failure), no interactive prompts, JSON config that plays nicely with variable substitution.
- **Self-updating** — `sqlci update-check` tells you when a new version is available with a direct download link.

## Installation

### 1. Download the latest release (recommended)

Go to the [latest release page](https://github.com/wshaddix/sqlci/releases/latest).

Download the file that matches your platform:

| Platform     | File name example                     | After download |
|--------------|---------------------------------------|----------------|
| Windows x64  | `sqlci-2.0.0-win-x64.exe`             | Rename to `sqlci.exe` (optional) and add to PATH |
| Linux x64    | `sqlci-2.0.0-linux-x64`               | `chmod +x`, rename to `sqlci`, add to PATH |
| macOS x64    | `sqlci-2.0.0-osx-x64`                 | `chmod +x`, rename to `sqlci`, add to PATH |

Each release also includes `SHA256SUMS.txt` for verification.

### 2. One-liner downloads (advanced)

**Linux / macOS (using GitHub API + jq):**

```bash
# Download the correct asset for your platform
curl -sL https://api.github.com/repos/wshaddix/sqlci/releases/latest \
  | jq -r '.assets[] | select(.name | endswith("-linux-x64")) | .browser_download_url' \
  | xargs curl -L -o sqlci

chmod +x sqlci
sudo mv sqlci /usr/local/bin/
```

Replace `-linux-x64` with `-osx-x64` for macOS. On Windows use PowerShell + `Invoke-RestMethod` or just download manually from the releases page.

**Even easier if you have the GitHub CLI installed:**

```bash
gh release download --latest --pattern "*-linux-x64" --output sqlci
chmod +x sqlci
```

### 3. Verify the install

```powershell
sqlci --version
sqlci update-check
```

## Getting Started

The fastest way to see SqlCi in action is to initialize a project, generate a script, and deploy it.

### 1. Initialize a new project

```powershell
sqlci init
```

Expected output:

```
Initializing new SqlCi project...
✓ Created config.json
✓ Created Scripts directory
✓ Created ResetScripts directory
✓ Created baseline script: 20250615143022123_all_baseline.sql

Initialization complete. Edit the baseline script and config.json to get started.
```

This creates:
- `config.json` with a single `local` environment
- `Scripts/` folder with a baseline script
- `ResetScripts/` folder (empty)

When you run `sqlci init` without the `--provider` flag in a normal terminal, SqlCi will interactively ask which database provider to use. The default (when run non-interactively or via scripts) is now **Sqlite** with a ready-to-use `local.db` file.

You can also specify the provider explicitly (useful in CI or scripts):

```powershell
sqlci init --provider Sqlite
sqlci init -p PostgreSql
sqlci init --provider SqlServer
```

> **Tip:** The generated `connectionString` is now a sensible default for the provider you selected. You will still want to review and customize it for your actual database/server.

### 2. Edit the baseline script and config

Open the generated baseline file (SqlCi tries to open it in your default `.sql` editor) and add something real:

```sql
-- 20250615143022123_all_baseline.sql
CREATE TABLE Customers (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    CreatedAt TEXT DEFAULT (datetime('now'))
);
```

**Important:** Edit `config.json` and replace the connection string with one that actually works for you. Example for a local SQLite file (when using `Sqlite` provider):

```json
"connectionString": "Data Source=local.db;Cache=Shared"
```

(For SQL Server use a valid LocalDB or server connection string; for Postgres use a `Host=...` Npgsql string.)

### 3. Generate a new environment-specific script

```powershell
sqlci generate local add_test_customers
```

Output:

```
✓ Created script: 20250615143105234_local_add_test_customers.sql
```

Add a few rows to the new file:

```sql
INSERT INTO Customers (Name) VALUES
    ('Alice Example'),
    ('Bob Test'),
    ('Charlie Dev');
```

### 4. Deploy

```powershell
sqlci deploy local
```

You'll see output similar to this (colors in a real terminal):

```
Verifying configuration ...
Configuration verification complete.
Deploying version 1.0.0 to local
Loading change script(s) from .\Scripts ...
Loaded 2 change script(s) from .\Scripts ...
20250615143022123_all_baseline.sql
20250615143105234_local_add_test_customers.sql
Checking for existance of script tracking table in the database ...
Script tracking table did not exist. Creating it now ...
Script tracking table was created ...
	Applying change script 20250615143022123_all_baseline.sql ...
	Applying change script 20250615143105234_local_add_test_customers.sql ...

Deployment Complete.
```

Exit code `0` on success, `-1` on any error.

> **Note:** The first deploy for an environment will create the tracking table. Subsequent deploys only run new scripts.

### 5. View history

```powershell
sqlci history local
```

Example:

```
Version  Date Ran                Script Name
-------  ----------------------  ---------------------------------------
1.0.0    6/15/2025 2:31:05 PM    20250615143022123_all_baseline.sql
1.0.0    6/15/2025 2:31:05 PM    20250615143105234_local_add_test_customers.sql

Current Database Version: 1.0.0 (6/15/2025 2:31:05 PM)
```

### 6. Check for updates anytime

```powershell
sqlci update-check
```

Real output when you're on the latest version:

```
Checking for updates...
You're running the latest version. (v2.0.0)
```

That's the happy path. You now have a repeatable, auditable deployment process for your SQL changes.

## Command Reference

### init

Creates a new `config.json`, `Scripts/`, `ResetScripts/`, and a baseline script.

```powershell
sqlci init
sqlci init --provider Sqlite
sqlci init -p PostgreSql
```

If you omit `--provider`, SqlCi will show an interactive selector in normal terminals (defaults to `Sqlite` in non-interactive environments such as CI).

Typical output (order is important — status first, then the checkmarks):

```
Initializing new SqlCi project...
✓ Created config.json
✓ Created Scripts directory
✓ Created ResetScripts directory
✓ Created baseline script: 20250615143022123_all_baseline.sql

Initialization complete. Edit the baseline script and config.json to get started.
```

### generate <environment> <script_name>

Creates a new timestamped script in the `Scripts` folder.

```powershell
sqlci generate local create_orders_table
sqlci generate qa seed_reference_data
```

The `<environment>` token becomes part of the filename (`_local_`, `_qa_`, etc.) and controls which deployments will pick up the script.

### deploy <environment>

Runs all new (not-yet-applied) scripts for the given environment.

```powershell
sqlci deploy local
sqlci deploy qa
sqlci deploy production
```

Behavior:
- Reads `config.json` and validates the target environment
- If `resetDatabase: true`, runs everything in `ResetScripts/` first (destructive)
- Creates the tracking table if missing
- Only executes scripts whose Id has not already been recorded for this environment
- Records every successful script run with the release version from config

### history <environment>

Shows every script that has been applied to the environment, plus the current database version.

```powershell
sqlci history production
```

### update-check

Checks GitHub for a newer release.

```powershell
sqlci update-check
sqlci update-check --prerelease     # include beta/rc versions
```

Typical output:

```
Checking for updates...
You're running the latest version. (v2.0.0)
```

## Configuration

All behavior is driven by `config.json` in the current directory.

Minimal example:

```json
{
  "scriptTable": "SchemaVersions",
  "version": "2.4.1",
  "scriptsFolder": "./Scripts",
  "resetScriptsFolder": "./ResetScripts",
  "environments": [
    {
      "name": "local",
      "connectionString": "Server=(localdb)\\\\MSSQLLocalDB;Database=MyApp_Local;Integrated Security=true;",
      "resetDatabase": true,
      "dbProvider": "SqlServer"
    },
    {
      "name": "production",
      "connectionString": "Server=prod-sql;Database=MyApp;User Id=app;Password=${PROD_DB_PASSWORD};",
      "resetDatabase": false,
      "dbProvider": "SqlServer"
    }
  ]
}
```

Key fields:

- `scriptTable` — name of the audit table created in the target database (default `ScriptTable`)
- `version` — the release/version string recorded with every script run
- `scriptsFolder` / `resetScriptsFolder` — relative to the directory you run `sqlci` from
- `environments[].name` — used for script filtering (`_all_` vs `_name_`)
- `environments[].dbProvider` — `SqlServer`, `PostgreSql`, or `Sqlite`

## Handling Secrets

Never commit real passwords. SqlCi supports two patterns:

### 1. Variable substitution (simple)

```json
"connectionString": "Server=...;Password=${PROD_DB_PASSWORD};"
```

Use `${VAR}` or `${env:VAR}`. If the variable is missing at runtime, SqlCi fails fast with a clear message.

### 2. Full connection string override (CI/CD recommended)

Set environment variables that completely replace the values in `config.json`:

- `SQLCI_PRODUCTION_CONNECTION`
- `SQLCI_PRODUCTION_RESET_CONNECTION`
- `SQLCI_LOCAL_CONNECTION`, etc.

This works great with GitHub Secrets, Azure Key Vault, AWS Secrets Manager, Doppler, 1Password CLI, etc.

## Script Naming & Execution

Scripts must follow this pattern:

```
<timestamp>_<all|environment>_<descriptive_name>.sql
```

Examples:

- `20250615143022123_all_baseline.sql`
- `20250615143105234_local_add_test_customers.sql`
- `20250615143210987_qa_seed_lookup_tables.sql`

Rules:
- Everything before the **first** `_` is treated as the unique script Id (stored in the tracking table).
- `_all_` scripts run in every environment.
- `_local_`, `_qa_`, `_prod_`, etc. scripts only run when that exact environment name is targeted (case-insensitive).
- Scripts execute in filename order (the timestamp format guarantees correct ordering).
- A script is never executed twice against the same environment.

You can still use any sortable prefix you like — the timestamp is just the default generated by `sqlci generate`.

## Supported Database Providers

| Provider    | Default batching                     | Notes |
|-------------|--------------------------------------|-------|
| `SqlServer` | Splits on `GO` (simple regex)        | Most mature path. Use `GO` between batches. |
| `PostgreSql`| Sends entire script as one command   | Supports `$$` dollar-quoted strings and functions. |
| `Sqlite`    | Sends entire script as one command   | Simple and fast for local/test workloads. |

Set the provider at init time or per environment with the `dbProvider` property.

## How It Works (briefly)

1. `sqlci deploy <env>` loads `config.json` and validates the target environment.
2. If reset is enabled, it runs every script in the reset folder against the reset connection.
3. It loads all matching `*.sql` files from the scripts folder.
4. It ensures a tracking table exists and finds which scripts have already run for this environment.
5. Only new scripts are executed, then recorded with the current `version`.
6. Every connection string and password is redacted before any output is written.

The model is deliberately simple and has worked reliably for many years across real CI/CD pipelines.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for build instructions, development workflow, and how releases are cut.

Full technical details for contributors and AI agents live in [AGENTS.md](AGENTS.md).
