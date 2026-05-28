# Sql CI

[![](http://i.imgur.com/g1WHerF.png)](http://www.ndepend.com)

A very simple sql script migration utility for continuous integration and automated deployments

# Features
- Automate SQL Server database deployments via command line utility (currently uses Microsoft.Data.SqlClient)
- Easily integrates with automated deployment solutions by using a JSON-based configuration file that supports an unlimited number of environments
- Uses exit codes (0 = success, -1 = failure) so automation tools can detect the result
- Optionally run drop/create database scripts (useful for developer workstations or test runs)
- Support scripts that need to change multiple databases (e.g. a script that runs against your app DB and also creates a SQL Agent job in msdb)
- Support running different scripts in different environments (e.g. data population scripts that differ between local/dev/qa/prod)

# Configuration
All configuration is done through the config.json file. This format was chosen over command line arguments simply because Octopus Deploy has built in support for modifying the .config files with variables that are specific to each environment.

## config.json
A typical `config.json` file looks like the following:
```json
{
  "scriptTable": "ScriptTable",
  "version": "1.0.0",
  "resetScriptsFolder": ".\\Reset",
  "scriptsFolder": ".\\Scripts",
  "environments": [
    {
      "name": "local",
      "resetConnectionString": "server=(localdb)\\\\MSSQLLocalDB; database=master; integrated security=true;",
      "connectionString": "server=(localdb)\\\\MSSQLLocalDB; database=MyDatabase_Local; integrated security=true;",
      "resetDatabase": true
    },
    {
      "name": "qa",
      "connectionString": "server=(localdb)\\\\MSSQLLocalDB; database=MyDatabase_Qa; integrated security=true;",
      "resetDatabase": false
    },
    {
      "name": "production",
      "connectionString": "Server=prod-sql;Database=MyDatabase_Production;User Id=appuser;Password=${env:PROD_DB_PASSWORD};",
      "resetDatabase": false
    }
  ]
}
```
**scriptTable** - The name of the table that should be used to store the scripts that have been ran. Defaults to "ScriptTable"

**version** - The version of the release to associate with the current script deployment. Defaults to "1.0.0"

**resetScriptsFolder** - The folder containing scripts used to reset (drop + recreate) the database. Defaults to `.\ResetScripts`. Relative to the current working directory when you run `sqlci`.

**scriptsFolder** - The folder containing your schema and data change scripts. Defaults to `.\Scripts`. Relative to the current working directory.

**environments** - SqlCi supports an unlimited number of target environments that you can deploy to. Each environment contains the following properties:

- **name** - The name of the environment (local, dev, qa, staging, production, etc.)
- **resetConnectionString** - The connection string to use when running scripts from the ResetScriptsFolder. This is typically different from your application's database.
- **connectionString** - The connection string used when running change scripts against the target database.
- **resetDatabase** - When `true`, the reset scripts (from `resetScriptsFolder`) will be executed against `resetConnectionString` before running the normal change scripts.
- **dbProvider** - The database provider for this environment (`SqlServer`, `PostgreSql`, or `Sqlite`). Defaults to `SqlServer`.

### Handling Secrets in Connection Strings

Connection strings often contain passwords or other secrets. SqlCi supports two mechanisms to avoid storing secrets directly in `config.json`:

#### 1. Environment Variable Substitution

You can reference environment variables using the `${env:VAR_NAME}` syntax (or the shorter `${VAR_NAME}`):

```json
{
  "environments": [
    {
      "name": "production",
      "dbProvider": "SqlServer",
      "connectionString": "Server=prod-sql;Database=MyApp;User Id=appuser;Password=${env:PROD_DB_PASSWORD};"
    }
  ]
}
```

At runtime, `${env:PROD_DB_PASSWORD}` will be replaced with the value of the `PROD_DB_PASSWORD` environment variable.

If the referenced environment variable is not set, SqlCi will throw a clear error during startup.

#### 2. Full Connection String Override

For even stronger secret isolation (especially useful in CI/CD), you can completely override a connection string using an environment variable:

- `SQLCI_<ENVIRONMENT>_CONNECTION` — overrides the main `connectionString`
- `SQLCI_<ENVIRONMENT>_RESET_CONNECTION` — overrides the `resetConnectionString`

Example:
```powershell
# In your CI pipeline or local environment
$env:SQLCI_PRODUCTION_CONNECTION = "Server=prod-sql;Database=MyApp;User Id=appuser;Password=...;"
```

When this variable is present, it completely replaces whatever is written in `config.json` for that environment.

This pattern works very well with secret managers (Azure Key Vault, AWS Secrets Manager, GitHub Secrets, 1Password CLI, Doppler, etc.).

# Usage

## Installation

### From GitHub Releases (recommended)

The easiest way for most users is to download a pre-built, single-file executable from the [latest release](https://github.com/wshaddix/sqlci/releases/latest).

1. Download the binary for your platform:
   - `sqlci-<version>-win-x64.exe` (Windows)
   - `sqlci-<version>-linux-x64` (Linux x64)
   - `sqlci-<version>-osx-x64` (macOS x64)
2. On macOS/Linux: `chmod +x sqlci-<version>-*`
3. (Optional) Rename it to `sqlci` (or `sqlci.exe`) and place it on your `PATH`.

Verify the installation:
```powershell
sqlci --version
sqlci update-check
```

Each release also includes `SHA256SUMS.txt` for verification and automatically generated release notes (a "What's Changed" list of commits and PRs since the previous tag).

### Other options

- **Run from source** (during development or when you want the absolute latest):
  ```powershell
  dotnet run --project src/SqlCi.Cli -- <command> [arguments]
  ```

- **Build your own binary** (see the publish script for the exact flags used in official releases):
  ```powershell
  dotnet publish src/SqlCi.Cli -c Release -r win-x64 --self-contained -o ./publish
  ./publish/sqlci.exe --help
  ```

- **As a .NET tool** (future): Packaging as a `dotnet tool` is possible but not yet set up.

## Getting Help
Run `sqlci --help`:

```
λ sqlci --help
USAGE:
    sqlci [OPTIONS] <COMMAND>

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information

COMMANDS:
    init                                    Initializes a new default config.json file and folders
    deploy <ENVIRONMENT>                    Deploy the database to the specified environment
    history <ENVIRONMENT>                   Show the history of scripts ran against an environment
    generate <ENVIRONMENT> <SCRIPT_NAME>    Generates a new script file
    update-check                            Check if a newer version of sqlci is available
```

> **Note:** The command-line interface was modernized in 2026 (subcommands instead of the old `-i` / `-d` / `-g` / `-h` flags). Older documentation and examples may still show the legacy syntax.

## Getting Started
To start a new project run `sqlci init`:
```
λ sqlci init
✓ Created config.json
✓ Created Scripts directory
✓ Created ResetScripts directory
✓ Created baseline script: 20260528150239506_all_baseline.sql

Initialization complete. Edit the baseline script and config.json to get started.
```

You can also specify the database provider upfront:
```
λ sqlci init --provider PostgreSql
```

This will create a new `config.json` file with sensible defaults (using the chosen provider). It will also create a `Scripts` folder and a `ResetScripts` folder. Within the `Scripts` folder it generates a baseline SQL script and attempts to open it in your default `.sql` editor.

### Supported Providers

| Value        | Database     | Notes |
|--------------|--------------|-------|
| `SqlServer`  | SQL Server   | Default. Uses `GO` as batch separator. |
| `PostgreSql` | PostgreSQL   | Standard `;` separated statements + dollar quoting for functions. |
| `Sqlite`     | SQLite       | Simple multi-statement execution. |

You can also change the provider per environment in `config.json` using the `dbProvider` property.

## Generating Scripts
To generate a new script run:
```
λ sqlci generate local add_test_users
✓ Created script: 20260528150312345_local_add_test_users.sql
```

The script will be created in the `Scripts` folder and named so that it only runs when the environment `local` is targeted. It will attempt to open the file in your default editor.

## Deploying Scripts
To deploy to an environment:
```
λ sqlci deploy local
```

Example output (with colors in a real terminal):
```
Verifying configuration ...
Configuration verification complete.
...
Deployment Complete.
```

On success the process exits with code `0`. On any error it exits with code `-1`.

## Showing History of Previous Deployments
```
λ sqlci history local
```

This prints a table of previously applied scripts for the given environment along with the current database version.

Example:
```
Version    Date Ran               Script Name
---------  ---------------------  ---------------------------------------
1.0.0      5/28/2026 3:15:52 PM   20231002140701805_all_add_initial_tables.sql
...

Current Database Version: 1.0.0 (5/28/2026 3:15:52 PM)
```

## Checking for Updates

You can manually check if a newer version of `sqlci` is available by running:

```
λ sqlci update-check
```

This command queries the latest release on GitHub and will tell you if an update is available, along with a direct link to download it.

Example output when an update exists:
```
A new version is available! Current: 1.0.0, Latest: 1.2.0
Download here → https://github.com/wshaddix/sqlci/releases/latest
```

# Script Naming Conventions
Every script must be named with a sequence number followed by an underscore followed by either the word "all" or the environment (dev|qa|prod|etc). An example would be

	20130717141326951_all_Create_Customer_Table.sql
	20130717141326952_all_Create_Order_Table.sql
	20130717141326953_all_Create_OrderItem_Table.sql
	20130717141326954_all_Create_States_Table.sql
	20130717141326955_dev_Populate_States_Table.sql
	20130717141326956_qa_Populate_States_Table.sql
	20130717141326957_prod_Populate_States_Table.sql


SqlCi will take the file name and strip the first N characters before the first underscore and use that as the sequence to sort by when running the scripts. Technically you can use any naming convention where the characters before the first underscore sorts sequentially. 

Next it will take every file that has "_all_" following the sequence number as well as scripts that have "_environment_" following the sequence number and run those scripts. The environment value is based on the name of the environment in your `config.json` file. The parameter value must match the naming convention for the script name. In the example file names above, all of the files with "_all_" in the name will be ran in every environment, and the file with "_dev_" (or "_local_", etc.) in the name will only be ran when there is an environment with a matching name in your `config.json` file.
