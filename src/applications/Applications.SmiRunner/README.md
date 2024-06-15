# SmiRunner

This CLI application serves as the single entry point for all other C# applications and services in the SmiServices repo.

## Supporting a new app or service

In order to add a new app or service to the runner, first create the csproj as normal in the appropriate "Applications" or "Microservices" directory/namespace.

It might be useful to first create this as a Console project, with a main entrypoint etc. for ease of initial testing. Once it's stable, change it to a library project by specifying `<OutputType>Library</OutputType>` in the csproj file.

To add the project to SmiRunner, the process is then to

- Add reference to the project in the SmiRunner csproj
- Add a new [ServiceVerb](./ServiceVerbs.cs)
- Add the new verb to [Program](./Program.cs)
  - Add to one of the static arrays `AllServices` or `AllApplications`
  - Add a case statement which points to the program entry point
