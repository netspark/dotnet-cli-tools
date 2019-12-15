# Netspark.CleanArchitecture.Scaffolder
This is a dotnet tool for generating clean architecture commands and queries along with respective unit test stubs.

## CLI Options
```
-c, config-file       Configuration file in yaml format for this tool (absolute or relative to the command process working directory)
-m, merge-strategy    Merge strategy for the scaffolding: Append|Overwrite|Skip
-o, output-folder     The root folder for commands/queries tree generation. (absolute or relative to the command process working directory)
-?, help              Show this help message
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
