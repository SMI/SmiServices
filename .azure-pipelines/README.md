# Azure Pipelines Build Notes

This directory contains the pipeline definitions and all other files used to test SmiServices.

## Pipelines

We currently have the following pipelines:
-   Windows build & test
-   Linux build & test

## Directory Contents

-   `docker-compose/` - docker-compose files and lockfiles (see below)
-   `jobs/` - job definitions
-   `scripts/` - scripts used by these pipelines
-   `steps/` - individual task step definitions
-   `*.yml` - The top-level pipeline definitions
-   `vars.json` - Variables for the pipelines

Note that yaml files which are used as templates have the extension `.tmpl.yml`.

## Variables

Variables for the pipelines are loaded from the `vars.json` file at the start of each run.

## Services

Service | Linux Provider | Windows Provider
 ------ | -------------- | ----------------
RabbitMQ | Docker | -
MongoDB | Docker | -
MsSQL | Docker | -
MariaDB | Docker | -
Redis | Docker | Unavailable

## Docker

We use Docker containers for running the external services wherever possible. This currently means "when on Linux", since the Azure Windows OS currently doesn't support WSL2 / running Linux containers. Other options could be investigated, e.g. see a similar discussion [here](https://github.com/opensafely/job-runner/issues/76).

The docker-compose files reference the `latest` tag for each image. During the pipeline run however, these are replaced with specific image digest versions using the [docker-lock](https://github.com/safe-waters/docker-lock) tool so the docker-compose files can be used as cache keys to enable repeatable builds.

## Caches

The [Cache](https://docs.microsoft.com/en-us/azure/devops/pipelines/release/caching?view=azure-devops) task is used in multiple cases to speed-up the build by re-using previously built/downloaded assets. These are restored based on the `key` value, which can contain references to files which are hashed to generate the final key.

## Notes

-   `set -x` in bash tasks may interfere with the `##vso` tasks, as AP will interpret any stdout containing that string as a command. This can cause problems with variables being set twice and having the wrong quote escaping. See:
    -   https://github.com/Microsoft/azure-pipelines-tasks/issues/10165
    -   https://github.com/microsoft/azure-pipelines-tasks/issues/10331
    -   https://developercommunity.visualstudio.com/content/problem/375679/pipeline-variable-incorrectly-inserts-single-quote.html
