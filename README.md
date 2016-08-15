# Sql CI

[![](http://i.imgur.com/g1WHerF.png)](http://www.ndepend.com)

A very simple sql script migration utility for continuous integration and automated deployments

# Features
- Automate database deployments to any ado.net data source via command line utility
- Easily integrates with automated deployment solutions by using a json based configuration file that supports unlimited number of environments. 
- Uses exit code so that various automation tools can know if it was successful or not. 
- Optionally run drop/create database scripts for when running on developer workstations or in between automated test runs
- Support scripts that need to change databases. For example a script that runs in "MyDatabase" but also needs to create a job via msdb
- Support running different scripts in different environments. For example you may have data population scripts that use different values between your workstation, dev, qa and production environments.

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
      "resetConnectionString": "server=(localdb)\\v11.0; database=master; integrated security=true;",
      "connectionString": "server=(localdb)\\v11.0; database=MyDatabase_Local; user=my_user; password=password1234;",
      "resetDatabase": "true"
    },
    {
      "name": "qa",
      "connectionString": "server=(localdb)\\v11.0; database=MyDatabase_Qa; integrated security=true;",
      "resetDatabase": "false"
    },
    {
      "name": "production",
      "connectionString": "server=(localdb)\\v11.0; database=MyDatabase_Production; integrated security=true;",
      "resetDatabase": "false"
    }
  ]
}
```
**scriptTable** - The name of the table that should be used to store the scripts that have been ran. Defaults to "ScriptTable"

**version** - The version of the release to associate with the current script deployment. Defaults to "1.0.0"

**resetScriptsFolder** - The name of the folder that holds the scripts to reset (drop/create) the database. Defaults to 
"ResetScripts". This should be a relative path from the directory where SqlCi.exe is ran from.

**scriptsFolder** - The name of the folder that holds the schema and data population scripts that should be ran against the database specified in the "Database" connection string setting. This should be a relative path from the directory where SqlCi.exe is ran from.

**environments** - SqlCi supports an unlimited number of target environments that you can deploy to. Each environment contains the following properties:

- **name** - The name of the environment (local, dev, qa, staging, production, etc)
- **resetConnectionString** - The connection string to use when running scripts from the ResetScriptsFolder. This is typically different from your application's database.
- **connectionString** - The connection string to use when running scripts
- **resetDatabase** - A boolean value that determines if the database should be reset (dropped/created) by running the scripts in the folder specified by the ResetScriptsFolder value.

# Usage

## Getting SqlCi
You can download the latest release [here](https://github.com/wshaddix/sqlci/releases/latest)

## Getting Help
Run `sqlci` with no options and it will show you the version and help information
```
λ sqlci
Version: 0.9.2.0
Usage: SqlCi.Console [OPTIONS]

Options:
  -i, --init                 Initializes a new default config.json file and
                               folders.
                               Usage: -i <database>
  -h, --history              Show the history of scripts ran.
                               Usage: -h <environment>
  -g, --generate             Generates a new script file.
                               Usage: -g <environment> <script_name>
  -d, --deploy               Deploy the database.
                               Usage: -d <environment>
```

## Getting Started
To start a new project run `sqlci -i`
```
λ sqlci -i
Created config.json
Created Scripts directory
Created ResetScripts directory
Created baseline script in Scripts directory
```
This will create a new `config.json` file with the defaults set. It will also create a `Scripts` folder and a `ResetScripts` folder. Within the `Scripts` folder it will generate a baseline sql script for you to start with and open it in the default editor associated with `.sql` files.

## Generating Scripts
To generate new sql scripts run `sqlci <environment> <script_name>`
```
λ sqlci -g dev add_test_users
```
This will generate a new script in the `Scripts` folder that will only run in an environment named `dev` in your `config.json` file. It will open the new script in the default editor associated with `.sql` files.

## Deploying Scripts
To deploy your database scripts to any environment defined in your `config.json` file run:
`sqlci -d <environment>`

```
λ sqlci -d local
Verifying configuration ...
Configuration verification complete.
Resetting the database ...
Loading reset script(s) from .\ResetScripts ...
Loaded 1 reset script(s) from .\ResetScripts ...
        20130927102554515_all_reset_database.sql
Resetting Database ...
        Applying reset script 20130927102554515_all_reset_database.sql ...
Opening connection to sql server using connection string: server=(localdb)\MSSQLLocalDB; database=master; integrated security=true; ...
Database reset complete.
Closing connection to sql server ...
Deploying version 1.0.0 to local
Loading change script(s) from .\Scripts ...
Loaded 15 change script(s) from .\Scripts ...
        20131002140701805_all_add_initial_tables.sql
        20131004134027913_all_update_disclaimer_text.sql
        20131007142210133_all_update_disclaimer_text_again.sql
        20131008085359619_all_add_user_table.sql
        20131008095141078_all_update_disclaimer_text_remove_invalid_characters.sql
        20131009164305242_all_update_disclaimer_text.sql
        20131011084332455_all_update_disclaimer_text.sql
        20131011101321030_all_update_disclaimer_text.sql
        20131011160549718_all_insert_new_disclaimer.sql
        20131016110234658_all_insert_new_disclaimer_text.sql
        20150512080243371_all_update_fcc_disclaimer.sql
        20150729120807888_all_add_admin_user.sql
        20150814091016781_all_update_disclaimers_to_v4.sql
        20160814125522310_all_add_test_table.sql
        20160814125730786_all_add_test_table2.sql
Checking for existance of script tracking table in the database ...
Opening connection to sql server using connection string: server=(localdb)\MSSQLLocalDB; database=MyDatabase_Local; integrated security=true; ...
Script tracking table did not exist. Creating it now ...
Script tracking table was created ...
15 new script(s) need to be applied ...
        Applying change script 20131002140701805_all_add_initial_tables.sql ...
        Applying change script 20131004134027913_all_update_disclaimer_text.sql ...
        Applying change script 20131007142210133_all_update_disclaimer_text_again.sql ...
        Applying change script 20131008085359619_all_add_user_table.sql ...
        Applying change script 20131008095141078_all_update_disclaimer_text_remove_invalid_characters.sql ...
        Applying change script 20131009164305242_all_update_disclaimer_text.sql ...
        Applying change script 20131011084332455_all_update_disclaimer_text.sql ...
        Applying change script 20131011101321030_all_update_disclaimer_text.sql ...
        Applying change script 20131011160549718_all_insert_new_disclaimer.sql ...
        Applying change script 20131016110234658_all_insert_new_disclaimer_text.sql ...
        Applying change script 20150512080243371_all_update_fcc_disclaimer.sql ...
        Applying change script 20150729120807888_all_add_admin_user.sql ...
        Applying change script 20150814091016781_all_update_disclaimers_to_v4.sql ...
        Applying change script 20160814125522310_all_add_test_table.sql ...
        Applying change script 20160814125730786_all_add_test_table2.sql ...
Closing connection to sql server ...
Deployment Complete.
```

## Showing History of Previous Deployments
To see a history of scripts ran against any environment defined in your `config.json` run `sqlci -h <environment>`
```
λ sqlci -h local
Verifying configuration ...
Configuration verification complete.
Opening connection to sql server using connection string: server=(localdb)\MSSQLLocalDB; database=MyDatabase_Local; integrated security=true; ...
Script tracking table already exists ...
Reading script run history ...
Closing connection to sql server ...

Version         Date Ran                        Script Name
=======         ========                        ===========
1.0.0           8/15/2016 9:15:52 AM            20131002140701805_all_add_initial_tables.sql
1.0.0           8/15/2016 9:15:52 AM            20131004134027913_all_update_disclaimer_text.sql
1.0.0           8/15/2016 9:15:52 AM            20131007142210133_all_update_disclaimer_text_again.sql
1.0.0           8/15/2016 9:15:52 AM            20131008085359619_all_add_user_table.sql
1.0.0           8/15/2016 9:15:52 AM            20131008095141078_all_update_disclaimer_text_remove_invalid_characters.sql
1.0.0           8/15/2016 9:15:52 AM            20131009164305242_all_update_disclaimer_text.sql
1.0.0           8/15/2016 9:15:52 AM            20131011084332455_all_update_disclaimer_text.sql
1.0.0           8/15/2016 9:15:52 AM            20131011101321030_all_update_disclaimer_text.sql
1.0.0           8/15/2016 9:15:52 AM            20131011160549718_all_insert_new_disclaimer.sql
1.0.0           8/15/2016 9:15:52 AM            20131016110234658_all_insert_new_disclaimer_text.sql
1.0.0           8/15/2016 9:15:52 AM            20150512080243371_all_update_fcc_disclaimer.sql
1.0.0           8/15/2016 9:15:52 AM            20150729120807888_all_add_admin_user.sql
1.0.0           8/15/2016 9:15:52 AM            20150814091016781_all_update_disclaimers_to_v4.sql
1.0.0           8/15/2016 9:15:52 AM            20160814125522310_all_add_test_table.sql
1.0.0           8/15/2016 9:15:52 AM            20160814125730786_all_add_test_table2.sql

Current Database Version: 1.0.0 (8/15/2016 9:15:52 AM)
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

Next it will take every file that has "_all_" following the sequence number as well as scripts that have "_environment_" following the sequence number and run those scripts. The environment value is based on the name of the environment in your `config.json` file. The parameter value must match the naming convention for the script name. In the example file names above all of the files with "_all_" in the name will be ran in every environment and the file with "_dev_" in the name will only be ran when there is an environment named _dev_ in your `config.json` file.
