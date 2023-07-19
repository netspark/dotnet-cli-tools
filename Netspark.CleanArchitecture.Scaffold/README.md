# Netspark.CleanArchitecture.Scaffolder
This is a dotnet tool for generating clean architecture commands, queries controllers along with respective unit and integration test stubs.

## CLI Options
```
a:gen-api                       Generate controllers and actions
c:config-file                   Configuration file in yaml format for this tool (absolute or relative to the command process working directory)
d:gen-commands                  Generate applicaiton commands
e:gen-events                    Generate events for executed commands
f:gen-full                      Generate all possible templates
h:gen-handlers                  Generate applicaiton command/query handlers
i:gen-integration-tests         Generate integration tests
m:merge-strategy                Merge strategy for the scaffolding: Append|Overwrite|Skip
o:output-folder                 The root folder for commands/queries tree generation (absolute or relative to the command process working directory)
q:gen-queries                   Generate applicaiton queries
t:templates-version             The version of template files used in scaffolder (v1 or v2)
u:gen-unit-tests                Generate unit tests
v:gen-validators                Generate fluent validators for commands or queries
x:gen-examples                  Generate swagger examples for controller actions
-?, help                        Show this help message
```

###  Config File Structure

There are 2 variants of the cleanasc.yaml file structure. 

### Variant 1: Only `Domains` Document

```
# Domains
---
Global:
  Commands:
    - CreateCountry
    - DeleteCountry
    - CreateLanguage
    - DeleteLanguage
    - CreateCurrency
    - RemoveCurrency
  Queries:
    - GetCurrenciesList
    - GetLanguagesList
    - GetCountriesList
Customer
  Commands:
    - UpsertCustomer
    - DeleteCustomer
  Queries:
    - GetCustomersList
    - GetCustomerDetail
```

### Variant 2: `Settings` and  `Domains` Documents

```
# Settings
---
Namespace: NorthwindTraders 
DbContext: NorthwindDbContext
SrcPath: ./Src
TestsPath: ./Tests
ApiUrlPrefix: api/v1

# Domains
---
Global:
  Commands:
    - CreateCountry
    - DeleteCountry
    - CreateLanguage
    - DeleteLanguage
    - CreateCurrency
    - RemoveCurrency
  Queries:
    - GetCurrenciesList
    - GetLanguagesList
    - GetCountriesList
Customer
  Commands:
    - UpsertCustomer
    - DeleteCustomer
  Queries:
    - GetCustomersList
    - GetCustomerDetail
```

`Settings.SrcPath` and `Settings.TestsPath` can be either absolute or relative to the folder, containing this yaml configuration.

Folder containing yaml config file is treated as source of the merge process, i.e. files in there will be probed for existence before executing merge strategy.
If passed in `config-file` is located in `output-folder`, the process will do merge as well as if you want to write generated files into another directory.

###  Examples

Command for generating new API controllers and appending actions to existing API controllers, with commands, queries, view models and swagger examples would look like this:

```
-c "c:/Path/To/Config/File/cleanasc.orders.yml" -o "c:/Path/To/My/Projects/MyProjectFolder" -adqx -t "v2" -m Append
```