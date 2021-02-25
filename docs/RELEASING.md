# SmiServices Release Process

The steps to cut a new release of SmiServices are as follows.

All development is done via a simple branching workflow, which are merged into `master` via a reviewed PR. `master` therefore contains all the latest reviewed changes since the previous release, and the CI checks should always be passing. It is not possible to push to `master` directly.

The release worflow is to checkout a new `release/` branch from master, update the `CHANGELOG` etc. as per below, then open a release PR with just those updates. Once this is merged, a tag is pushed to `master`. This triggers a pipeline in Azure DevOps which creates a GitHub release. The other pipelines will then push artefacts to this release when they pass.

## Creating A Normal Release

-   Check that the CHANGELOG is up-to-date. To do this, checkout the latest `master` commit and list all the merged PRs since the last release, e.g.:
    ```console
    $ git checkout master && git pull
    $ git log --merges --oneline <previous tag>.. | grep -v dependabot
    ec182696 Merge pull request #430 from SMI/feature/extraction-fixes
    051a134e Merge pull request #444 from SMI/feature/trigger-updates
    65fcfe41 Merge pull request #440 from SMI/feature/value-updater
    8515f059 Merge pull request #438 from SMI/feature/consensus-tidyup
    38e25c2a Merge pull request #434 from SMI/feature/consensus-rule
    9d81b942 Merge branch 'master' into develop
    ee39d850 Merge pull request #408 from SMI/feature/isidentifiable-more-logs
    c709f7ed Merge pull request #404 from SMI/feature/no-an-suffix
    d7d90f4a Merge pull request #402 from SMI/feature/update-docs
    830bac67 Merge branch 'master' into develop
    ```
    Go through these PRs on GitHub and check each has an accurate CHANGELOG entry.

-   Identify the next release version. This can be determined by looking at the previous release and deciding if the new code to be released is a major, minor, or patch change as per [semver](https://semver.org). E.g. if the previous release was `v1.2.3` and only new non-breaking features are in the `Unreleased` section of the CHANGELOG, then the next release should be`v1.3.0`. The definition of "breaking" can often be subjective though, so ask other members of the project if you're unsure.

-   Ensure you are on the latest commit on the `master` branch , and create a new release branch:

    ```console
    $ git fetch
    $ git status
    On branch master
    Your branch is up to date with 'origin/master'.

    nothing to commit, working tree clean

    $ git checkout -b release/v1.2.3
    Switched to a new branch 'release/v1.2.3'
    ```

-   Update the [CHANGELOG](/CHANGELOG.md) for the new release. This involves adding a new header and link for the release tag. See [this](https://github.com/SMI/SmiServices/commit/d98af52960214b6ae2c26510e3310f10038b494d) commit as an example

-   Update any other files referencing the version. To see an example, check the previous release PR. At time of writing, these are:
    -   `README.md`: Bump the version in the header
    -   `src/SharedAssemblyInfo.cs`: Bump the versions in each property

-   Commit these changes and push the new branch with the message "Start release branch for v1.2.3"
-   Open a PR for this branch with the title `Release <version>`. Request a review from `@tznind` and `@rkm`
-   If there are any further changes which need to be included in the release PR, then these can be merged into the release branch from `master`
-   Wait for the PR to be reviewed and merged
-   Checkout `master` and pull the merge commit
-   Tag the release, e.g.:
    ```console
    $ git tag v1.2.3
    $ git push origin v1.2.3
    ```
-   Delete the release branch
-   Wait for Azure Pipelines to build the release
-   Check that the built binaries are added to the [releases](https://github.com/SMI/SmiServices/releases) page.

## Creating A Hotfix Release

Hotfixes are small patches which are created in response to some show-stopper bug in the previous release.

The process is similar to above, except:

-   The branch name should be `hotfix/v...`
-   The commit message should be "Start hotfix branch for v1.2.3"
-   The PR should be titled `Hotfix <version>`
