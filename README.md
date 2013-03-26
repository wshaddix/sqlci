# Sql CI


A very simple sql script migration utility for continuous integration and automated deployments

# Goals
* Automate database deployments to MS Sql Server via command line utility
* Integrate with Octopus Deploy but not be dependent on it or integrated with it so that the utility can work without it
* Run in multiple environments (Dev, QA, Staging, Production)
* Roll back changes based on either a release number or a script number (can associate a batch of scripts with a single release number i.e. 1.3.4 could be associated with 15 sql scripts)
* Be exit code friendly so that various automation tools can know if it was successful or not (don't just depend on console output)
* Optionally run drop/create database scripts for when running on developer workstations or in between automated test runs
