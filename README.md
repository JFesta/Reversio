# Reversio
Entity Framework Core Reverse-Engineering Cli/Library Tool

Reversio is a tool designed to improve over Microsoft's somewhat limited [DbContext-Scaffold](https://docs.microsoft.com/en-us/ef/core/get-started/aspnetcore/existing-db) reverse engineering command and offer a simple-to-execute and fully configurable solution to let you reverse engineer your Sql Server database in a fast and reliable way.

Key features:
* Settings file based: the program parses and executes a list of **jobs** defined in one or more json-formatted settings files; you'll keep these files versioned with your code
* Schema selection: you can choose which schema to read from; objects in non-specified schemas will be ignored
* Exclusion filters: after you have selected the schemas, you can exclude selected objects from being generated, based on type (table/view) or name (you can specify the exact object names as well as regexes)
* Custom namespace and location to store your POCOs and your DbContext separately
* Custom class/property name transform: replace by regex, force PascalCase names, etc.
* Custom inheritance: POCOs and DbContext can extend any specified interface/class
* Support for Lazy Loading: you can generate navigation properties as **virtual** members
* Indices: you can optionally include their definition in your DbContext

# Supported DBMSs/ORMs

At the moment Reversio supports only Sql Server databases (tested with 2012 and 2016).
The generated DbContext is tailored specifically to EF Core, but a second version for EF can be done too if needed.

However, as the generated POCOs carry no dependencies by default, you can use Reversio to just generate them and not the DbContext; in this way you can use the POCOs with other ORMs, such as Dapper.

# Dependencies

Work in progress

# What's next

### Planned features:
* Automatically ignore history tables connected to temporal tables
* DbContext stubs to let custom code injections
* Support for Query Types 
* Support for scalar/table functions and stored procedures

### Possible next features:
* Support for MySql
* Support for Oracle
