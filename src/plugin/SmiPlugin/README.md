You can build the SmiPlugin for RDMP using the following:

```
dotnet publish
nuget pack ./SmiPlugin.nuspec -Properties -IncludeReferencedProjects -Symbols -Version 0.0.1
```