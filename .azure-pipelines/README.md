# Azure Pipelines Build Notes

This directory contains the Azure Pipelines definitions and all other files used to test SmiServices.

## Pipelines

We currently have the following pipelines:
-   [Linux test & package](https://dev.azure.com/smiops/Public/_build?definitionId=3). Defined [here](/.azure-pipelines/linux.yml). Runs the C# and Java tests on Linux. If the build is for a tag, also packages both sets of services and uploads them to a GitHub release
-   [Windows test & package](https://dev.azure.com/smiops/Public/_build?definitionId=4). Defined [here](/.azure-pipelines/windows.yml). Same as above, but on Windows. Runs a reduced set of tests, since some services are not available (see below).
-   [Create GitHub Release](https://dev.azure.com/smiops/Public/_build?definitionId=5). Defined [here](/.azure-pipelines/create-gh-release.yml). Runs automatically for tags on master to create a GitHub release which the other pipelines will publish packages to

## Directory Contents

-   `docker-compose/` - docker-compose files and lockfiles (see below)
-   `jobs/` - job definitions
-   `scripts/` - scripts used by these pipelines
-   `steps/` - individual task step definitions
-   `*.yml` - The top-level pipeline definitions
-   `vars.json` - Variables for the pipelines

Note that yaml files which are used as templates have the extension `.tmpl.yml`.

## Variables

Variables for the pipelines are loaded from the `vars.json` file at the start of each run. These are often combined with [pre-defined variables](https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables).

## Services

Service | Linux Provider (`ubuntu-18.04`) | Windows Provider (`windows-2019`)
 ------ | -------------- | ----------------
RabbitMQ | Docker | -
MongoDB | Docker | pre-installed
MsSQL | Docker | [SqlLocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15)
MariaDB | Docker | -
Redis | Docker | Unavailable

## Docker

We use Docker containers for running the external services wherever possible. This currently means "when on Linux", since the Azure Windows OS currently doesn't support WSL2 / running Linux containers. Other options could be investigated for Windows, e.g. see a similar discussion [here](https://github.com/opensafely/job-runner/issues/76).

The docker-compose files reference the `latest` tag for each image. During the pipeline run however, these are replaced with specific image digest versions using the [docker-lock](https://github.com/safe-waters/docker-lock) tool. This is so the docker-compose files can be used as cache keys to enable repeatable builds.

## Caches

The [Cache](https://docs.microsoft.com/en-us/azure/devops/pipelines/release/caching?view=azure-devops) task is used in multiple cases to speed-up the build by re-using previously built/downloaded assets. These are restored based on the `key` value, which can contain references to files which are hashed to generate the final key.

## Notes

-   `set -x` in bash tasks may interfere with the `##vso` syntax, as AP will interpret any stdout containing that string as a command. This can cause problems with variables being set twice and having the wrong quote escaping. See:
    -   https://github.com/Microsoft/azure-pipelines-tasks/issues/10165
    -   https://github.com/microsoft/azure-pipelines-tasks/issues/10331
    -   https://developercommunity.visualstudio.com/content/problem/375679/pipeline-variable-incorrectly-inserts-single-quote.html

-   Dealing with variables containing path separators across multiple platforms (i.e. Linux and Windows) can be tricky. Variables pre-defined by AP will have the correct separators for the current OS. When used in `Bash` tasks, it's safe enough for either OS to just use Linux (`/`) separators when combining paths, so long as they are always quoted. However, if the variable is also used as part of a cache `path`, it may need to be manually re-created with Windows-style (`\`) separators before use.