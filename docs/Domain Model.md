# Domain Model

## Project Configuration

A project configuration is the top-level data that represents the starting point for SqlCi. Everything that is tracked and configured starts with a project configuration. There is a single instance of a project configuration and it is always named `project.config` and stored alongside the `sqlci` executable and checked into source control. A project can consist of one or more databases where each database is of a specified type and includes one or more environments in which the database is deployed to

| Attribute | Data Type                        | Required | Constraints                                                  | Notes                                                        |
| --------- | -------------------------------- | -------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Name      | string                           | Yes      | Must be a valid filename for the filesystem that it will be stored on | The name will be sanitized and made safe for using as a file name for the given OS (Windows, Linux, MacOS) |
| Databases | Array of `DatabaseConfiguration` | Yes      |                                                              |                                                              |



## Database Configuration

A database configuration belongs to a project and represents a named database along with it's type (sqlite, mysql, mssql, etc). The type let's us know which ADO.Net driver to load when connecting to and running scripts against the database. Each database configuration is stored in a separate directory on the file system as a child to the project directory. 

| Attribute    | Data Type                      | Required | Constraints         | Notes                                                        |
| ------------ | ------------------------------ | -------- | ------------------- | ------------------------------------------------------------ |
| Name         | string                         | Yes      |                     | The name of the database. This is only used for display purposes in the log messages |
| ScriptTable  | string                         | Yes      | Min:5<br />Max: 100 | The name of the table in the target database that will hold the audit messages of scripts ran against it. This table is used for determining which sql scripts need to be ran for this migration as well as to show the audit history of scripts that have been ran in the past |
| DbType       | string                         | Yes      |                     | The type of database. This determines which ADO.Net driver we use to connect to the database. |
| Environments | Array of `DatabaseEnvironment` | Yes      |                     | A collection of different environments that this database is deployed to along with it's connection details |



## Database Environment

A database environment represents a physical location/machine where the database is deployed (or will be deployed). This includes an easy to remember name (workstation, dev, qa, prod, etc) as well as the connection information for that location.

| Attribute        | Data Type | Required | Constraints | Notes                                                        |
| ---------------- | --------- | -------- | ----------- | ------------------------------------------------------------ |
| Name             | string    | Yes      |             | The name of the database. This is only used for display purposes in the log messages |
| ConnectionString | string    | Yes      |             | The ADO.Net connection string used to connect to the database. Permissions should be such that all scripts (DDL & DML) can be executed. |
