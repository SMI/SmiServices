#!/usr/bin/env bash

set -euxo pipefail

dotnet tool update --global coveralls.net

if [ ! -z "$(System.PullRequest.PullRequestNumber)" ]
then
    commit_id_arg="--commitId $(System.PullRequest.SourceCommitId)"
    commit_branch_arg="--commitBranch $(System.PullRequest.SourceBranch)"
    pull_request_arg="--pullRequest $(System.PullRequest.PullRequestNumber)"
else
    commit_id_arg="--commitId $(Build.SourceVersion)"
    commit_branch_arg="--commitBranch $(Build.SourceVersion)"
    pull_request_arg=""
fi

csmacnz.Coveralls \
    --opencover \
    -i coverage/coverage.opencover.xml \
    --useRelativePaths \
    $commit_id_arg \
    $commit_branch_arg \
    $pull_request_arg \
    --commitAuthor "$(git show -s --format='%an')" \
    --commitEmail "$(git show -s --format='%ae')" \
    --commitMessage "$(Build.SourceVersionMessage)" \
    --jobId "$(Build.BuildId)"
