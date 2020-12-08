# SmiServices Release Process

The steps to cut a new release of SmiServices are as follows.

All normal releases should come from the `develop` branch, which new features and changes are merged into after review. The release worflow is to create a `release/vX.Y.Z` branch from `develop` which is reviewed via PR, merged into `master`, and tagged.

One exception are `hotfix` releases, which are branches created directly from `master` and merged back as a new release in order to address a critical bug in the previous release.

## Creating A Normal Release

-   Check that the CHANGELOG is up-to-date. To do this, checkout the latest develop commit and list all the merged PRs since the last release, e.g.:
    ```console
    $ git fetch origin
    $ git log --merges --first-parent origin/develop --oneline v1.12.2..origin/develop | grep -v dependabot
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

-   Identify the next release version. This can be determined by looking at the previous release and deciding if the new code to be released (from the `develop` branch) is a major, minor, or patch change as per [semver](https://semver.org). E.g. if the previous release was `v1.2.3` and only new non-breaking features are in the `Unreleased` section of the CHANGELOG, then the next release should be`v1.3.0`. The definition of "breaking" can often be subjective though, so ask other members of the project if you're unsure.

-   Ensure you are on the latest commit on the `develop` branch , and create a new release branch:

    ```console
    $ git status
    On branch develop
    Your branch is up to date with 'origin/develop'.

    nothing to commit, working tree clean

    $ git pull
    Already up to date.

    $ git checkout -b release/v1.2.3
    Switched to a new branch 'release/v1.2.3'
    ```

-   Update the CHANGELOG for the new release. This involves adding a new header and link for the release tag. See [this](https://github.com/SMI/SmiServices/commit/c8e198f937779debcc65b72d074b9bce7dad4691#diff-06572a96a58dc510037d5efa622f9bec8519bc1beab13c9f251e97e657a9d4ed) commit as an example
-   Update any other files referencing the version. To see an example, check the previous release commit: `git log --all --grep="Start release branch" -1 --name-only --format=`. E.g.:
    -   `README.md`: Bump the version
    -   `src/SharedAssemblyInfo.cs`: Bump the versions

-   Commit these changes and push the new branch with the message "Start release branch for v1.2.3"
-   Open a PR for this branch with the title "Release <version>". Request a review from `@tznind` and `@rkm`
-   If there are any further changes which need to be included in the release, then these can be merged into the release branch from `develop`
-   Wait for the PR to be reviewed and merged
-   Checkout `master` and pull the merge commit
-   Tag the release, e.g.:
    ```console
    $ git tag v1.2.3
    $ git push origin v1.2.3
    ```

-   Merge `master` back into `develop` to ensure that any changes from the release branch are present in develop
-   Delete the release branch
-   Wait for Travis to build the tagged commit
-   Check that the built binaries are added to the [releases](https://github.com/SMI/SmiServices/releases) page. Update the title and description using the CHANGELOG.

## Creating A Hotfix Release

TODO
