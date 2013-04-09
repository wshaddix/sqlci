# Sql CI


A very simple sql script migration utility for continuous integration and automated deployments

# Features
- Automate database deployments to MS Sql Server via command line utility
- Easily integrates with automated deployment solutions by accepting configuration using either a .config file (for use with octopus deploy) or from command line arguments (for nant, batch scripts, etc). 
- Use exit code so that various automation tools can know if it was successful or not (don't just depend on console output)
- Optionally run drop/create database scripts for when running on developer workstations or in between automated test runs
- Support scripts that need to change databases. For example a script that runs in "MyDatabase" but also needs to create a job via msdb
- Support running different scripts in different environments. For example you may have data population scripts that use different values between your workstation, dev, qa and production environments.

**Non-Goal** - A specific non-goal is to support scripts that use the GO keyword to separate statements that are dependent on the prior statement being committed to the database. It's far easier to just make multiple scripts and makes the development of the tool easier to maintain and reduces dependencies.

# Configuration
All configuration is done through the SqlCi.Console.exe.config file. This format was chosen over command line arguments simply because Octopus Deploy has built in support for modifying the .config files with variables that are specific to each environment/role and it also does not exclude the tool being used by other automation platforms. You'll need to specify a connection string to the database to run the scripts against as well as some appSetting entries as shown below.

## Connection String
The connection string should be specified under the connectionStrings element and have a name of "Database"
```csharp
<connectionStrings>
	<add name="Database" connectionString="server=myserver; database=mydatabase; user=myuser; password=mypassword;"/>
</connectionStrings>
```

## App Settings

**ScriptTable** - The name of the table that should be used to store the scripts that have been ran. Defaults to "ScriptTable"

**ReleaseVersion** - The version of the release to associate with the current script deployment. Defaults to "1.0.0"

**ResetScriptsFolder** - The name of the folder that holds the scripts to reset (drop/create) the database. Defaults to "ResetScripts". This should be a relative path from the directory where SqlCi.Console.exe is ran from.

**Environment** - The environment that the scripts are being ran in. Used when selecting which sql scripts to run.

**ResetDatabase** - A boolean value that determines if the database should be reset (dropped/created) by running the scripts in the folder specified by the ResetScriptsFolder value.

**ScriptsFolder** - The name of the folder that holds the schema and data population scripts that should be ran against the database specified in the "Database" connection string setting. This should be a relative path from the directory where SqlCi.Console.exe is ran from. 

# Usage
## If using SqlCi.Console.exe.config
Once the SqlCi.Console.exe.config file has been setup with the appropriate connection string and apps settings simply run SqlCi.Console.exe with the -uc option. This instructs SqlCi.Console.exe to read it's configuration from the configuration file instead of parsing command line arguments. Output will look similar to the following:

	c:\> SqlCi.Console.exe -uc

	Verifying configuration ...
	Configuration verification complete. Starting deployment ...
	Loading change script(s) from .\Scripts ...
	Loaded 6 change script(s) from .\Scripts ...
	Checking for existance of script tracking table in the database ...
	Opening connection to sql server using connection string: server=sqlserver2012; database=esbsecurity; user=wshaddix; password=Airplane500; ...
	Script tracking table already exists ...
	Checking to see which change script(s) have already been applied ...
	Found 5 change script(s) that have already been applied ...
	Calculating which new change script(s) need to be applied ...
	1 new change script(s) need to be applied ...
	Applying change script 0006_More_Stuff.sql ...
	Deployment complete.
	Closing connection to sql server ...


## If using command line arguments
If you run SqlCi.Console.exe --help you will get the list of options for configuring via the command line. 

	c:\> SqlCi.Console.exe --help
	Usage: SqlCi.Console [OPTIONS]
	Runs a set of sql scripts against the specified database.
	
	Options:
	      --uc, --useConfig      
								 Determines whether to get config values from the SqlCi.Console.exe.config file or the command line arguments
	      --cs, --connectionString=VALUE
	                             The connection string to use to access the database to run the scripts in
	      --st, --scriptTable=VALUE
	                             The name of the script table
	      --rv, --releaseVersion=VALUE
	                             The version of this release
	      --sf, --scriptsFolder=VALUE
	                             The folder that holds the sql scripts to be ran
		  --ev, --environment=VALUE
								 The environment that the scripts are being ran in
	      --rd, --resetDatabase  
								 Determines if the database should be reset
	      --rf, --resetFolder=VALUE
	                             The folder that holds the database reset scripts to be ran if resetDatabase is specified
	  	  -h, --help                 
								 show this message and exit

An example of configuring from the command line would look like:

	c:\> SqlCi.Console.exe -cs="server=sqlserver2012; database=esbsecurity; user=wshaddix; password=Airplane500;" -sf=".\Scripts" -rv="1.0.0" -st=ScriptTable -ev=dev

# Script Naming Conventions
Every script must be named with a sequence number followed by an underscore followed by either the word "all" or the environment (dev|qa|prod|etc). An example would be

	0001_all_Create_Customer_Table.sql
	0002_all_Create_Order_Table.sql
	0003_all_Create_OrderItem_Table.sql
	0004_all_Create_States_Table.sql
	0005_dev_Populate_States_Table.sql
	0005_qa_Populate_States_Table.sql
	0005_prod_Populate_States_Table.sql


SqlCi will take the file name and strip the first N characters before the first underscore and use that as the sequence to sort by when running the scripts. Technically you can use any naming convention where the characters before the first underscore sorts sequentially. 

Next it will take every file that has _all_ following the sequence number as well as scripts that have _<environment>_ following the sequence number and run those scripts. The environment value is based on the -ev parameter passed to SqlCi.exe. The parameter value must match the naming convention for the script name.
