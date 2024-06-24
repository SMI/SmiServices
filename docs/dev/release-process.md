# SmiServices Release Process

The steps to cut a new release of SmiServices are as follows.

All development is done via a simple branching workflow, which are merged into `main` via a reviewed PR. `main` therefore contains all the latest reviewed changes since the previous release, and the CI checks should always be passing. It is not possible to push to `main` directly.

The release workflow is to checkout a new `release/` branch from main, update the `CHANGELOG` etc. as per below, then open a release PR with just those updates. Once this is merged, a tag is pushed to `main`. This triggers a pipeline in Azure DevOps which creates a GitHub release. The other pipelines will then push artefacts to this release when they pass.

## Creating A Normal Release

- Review all open PRs and check if any have been approved and can be merged to be included in the release.

- Check that a [news file][news_files] is present for each merged PR since the previous release. To do this, checkout the latest `main` commit and list all the merged PRs since the last release, e.g.:

  ```console
  $ git checkout main && git pull && ./bin/release/missing-news.sh
  Missing news file for PR #1151:Explicitly initialise LocalDB on Windows
  Missing news file for PR #1042:Is ident package use
  Missing news file for PR #1079:bump all HIC libraries to latest, and fo-dicom to 4.0.8
  Missing news file for PR #1057:[Snyk] Security upgrade edu.stanford.nlp:stanford-corenlp from 3.9.2 to 4.3.1
  ```

  Go through these PRs and check each has an accurate [news file][news_files] entry. Create any missing files if needed.

- Identify the next release version. This can be determined by looking at the previous release and deciding if the new code to be released is a major, minor, or patch change as per [semver](https://semver.org). E.g. if the previous release was `v1.2.3` and only new non-breaking features are in the news files directory, then the next release should be`v1.3.0`. The definition of "breaking" can often be subjective though, so ask other members of the project if you're unsure.

- Ensure you are on the latest commit on the `main` branch , and create a new release branch:

  ```console
  $ git fetch
  $ git status
  On branch main
  Your branch is up to date with 'origin/main'.

  nothing to commit, working tree clean

  $ git checkout -b release/v1.2.3
  Switched to a new branch 'release/v1.2.3'
  ```

- Update the [CHANGELOG](/CHANGELOG.md) for the new release. This involves running `./bin/release/updateChangelog.py <prev version> <next version>`. Review the diff and check for any obvious errors.

- Update any other files referencing the version. To see an example, check the previous release PR. At time of writing, these are:

  - `src/SharedAssemblyInfo.cs`: Bump the versions in each property

- Commit these changes and push the new branch

- Open a PR for this branch with the title `Release <version>`. Request a review from `@tznind` and `@rkm`

- If there are any further changes which need to be included in the release PR, then these can be merged into the release branch from `main`

- Wait for the PR to be reviewed and merged

- Checkout `main` and pull the merge commit

- Tag the release, e.g.:

  ```console
  $ git tag v1.2.3
  $ git push origin v1.2.3
  ```

- Delete the release branch

- Wait for Azure Pipelines to build the release

- Check that the built binaries are added to the [releases](https://github.com/SMI/SmiServices/releases) page.

- (Internal) Ping the Mattermost ~developers channel to let everyone know there is a release available, and to not start any long-running tasks

## Creating A Hotfix Release

Hotfixes are small patches which are created in response to some show-stopper bug in the previous release.

The process is similar to above, except:

- The branch name should be `hotfix/v...`
- The PR should be titled `Hotfix <version>`

<!-- Links -->

[news_files]: /news/README.md
