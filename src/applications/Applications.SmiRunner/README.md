# SmiRunner

This CLI application serves as the single entry point for all other C# applications and servcices in the SmiServices repo.

## Usage

Running `--help` will show a list of all the services, and a link to the main README for that service

```console
$ ./smi --help
smi 1.15.1
Copyright  SMI Project 2018-2020

  trigger-updates              See here at your release version:
                               https://github.com/SMI/SmiServices/tree/main/src/applications/Applications.TriggerUpdates

  ...
```

Each service can then be run the same as previously by passing its own specific set of parameters or verbs

```console
$ ./smi dicom-tag-reader -y foo.yaml
...
```

```console
$ ./smi is-identifiable db -y default.yaml ...
...
```

## Supporting a new app or service

In order to add a new app or service to the runner, first create the csproj as normal in the appropriate "Applications" or "Microservices" directory/namespace.

It might be useful to first create this as a Console project, with a main entrypoint etc. for ease of initial testing. Once it's stable, change it to a library project by specifying `<OutputType>Library</OutputType>` in the csproj file.

To add the project to SmiRunner, the process is then to

-   Add reference to the project in the SmiRunner csproj
-   Add a new [ServiceVerb](./ServiceVerbs.cs)
-   Add the new verb to [Program](./Program.cs)
    -   Add to one of the static arrays `AllServices` or `AllApplications`
    -   Add a case statement which points to the program entry point
