# SmiServices Release Process

The steps to cut a new release of SmiServices are as follows

## Creating A Normal Release

-   First, identify the next release version. This can be determined by looking at the last release and deciding if the new code to be released is a major, minor, or patch change as per [semver](https://semver.org). E.g. if the previous release was `v1.2.3` and only new non-breaking features are in the `Unreleased` section of the CHANGELOG, then the next release should be`v1.3.0`. The definition of "breaking" can often be subjective though, so other members of the team if you're unsure.

-   Ensure you are on the latest commit on the `develop` branch , and create a new release branch:

    ```console
    $ git st
    On branch develop
    Your branch is up to date with 'origin/develop'.

    nothing added to commit but untracked files present (use "git add" to track)

    $ git pull
    Already up to date.

    $ git checkout -b release/v1.3.0
    Switched to a new branch 'release/v1.3.0'
    ```

-   Update any files referencing the version. To see an example, check the previous release commit: `git log --all --grep="Start release branch" -1 --name-only --format=`. E.g.:
    -   `CHANGELOG.md`: Add a new section header under `Unreleased`, and add a new link to the bottom
    -   `README.md`: Bump the version
    -   `src/SharedAssemblyInfo.cs`: Bump the versions

-   Commit these changes and push the new branch with the message "Start release branch for v1.2.3"
-   Open a PR for this branch with the title "Release <version>". Request a review from `@tznind` and `@rkm`.
-   If there are any further changes which need to be included in the release, then these can be merged into the release branch from `develop`.
-   Wait for the PR to be reviewed and merged
-   Checkout `master` and pull the merge commit
-   Tag the release, e.g.:

    ```console
    $ git tag v1.2.3
    $ git push origin v1.2.3
    ```

-   Merge `master` back into `develop` to ensure that any changes from the release branch are present
-   Delete the release branch
-   Wait for Travis to build the tagged commit
-   Check that the built binaries are added to the [releases](https://github.com/SMI/SmiServices/releases) page. Update the title and description using the CHANGELOG.

## Creating A Hotfix Release

TODO
