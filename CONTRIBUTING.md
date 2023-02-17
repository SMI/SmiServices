# SMI Services Contributing Guidelines

The SMI Services project team uses the Feature Branch Workflow to accomplish work in a consistent and productive manner. It is a Git workflow that uses feature branches to isolate new features and bug fixes from the main codebase, allowing multiple developers to work on the same codebase without causing conflicts or disrupting the main codebase.

This workflow works if you are a member of the SMI organisation, or a direct member of the repo, however, external contributors will need to fork the repo first as described here: [GitHub Fork a repo](https://docs.github.com/en/get-started/quickstart/fork-a-repo). Once forked, the Feature Branch Workflow remains the same as desrcibed below.

**Feature Branch Workflow**: 
- Clone Project
- Create Branch
- Add/Commit Changes
- Push Changes
- Open Pull Request
- Request Review
- Merge Changes
- Repeat Process

Here is an example of a typical Feature Branch Workflow whilst working on the SMI project:

1. **Clone Project**: As an individual developer (contributor) clone the central repository onto your local machine.

    ```console
    git clone git@github.com:SMI/SmiServices.git
    ```

2. **Create Branch**: For each new feature or bug fix, create a new branch from the main branch (usually called “master”) which will allow you to work on your own version of the code without affecting the main codebase.

    To keep the codebase organised, it is a good idea to use a descriptive name and consistent format for branch names. One common format is to use a prefix to indicate the type of branch, such as "feature/" for new features, "bug/" for bug fixes, or "hotfix/" for critical fixes e.g., feature/add-user-authentication or bug/fix-duplicate-messages.

    ```console
    git checkout -b new-feature
    ```

    This creates a new branch called "new-feature" and switches to that branch.

3. **Add/Commit Changes**: You can make changes, add new code, and commit those changes to your local repository.

    ```console
    git add -u # To stage changes to existing files
    git add file1 file2 ... # Alternatively, to add new files

    git commit -m "Update/Add new feature X"
    ```
    This adds and commits the changes to the local repository.

4. **Push Changes**: Once you have committed the changes to your local repository, push your branch to the central repository.

    ```console
    git push origin new-feature
    ```

    This pushes the "new-feature" branch to the central repository.

5. **Open Pull Request**: In your Git hosting service (e.g. GitHub/GitLab), open a pull request to merge the "new-feature" branch into the main "master" branch.

6. **Request Review**: Other team members will review the code and provide feedback. If any issues are found, you can fix them and push the updated changes to the branch.

    ```console
    git add -u # To stage changes to existing files
    git add file1 file2 ... # Alternatively, to add new files

    git commit -m "Fixed issue found in code review"
    git push origin new-feature
    ```

7. **Merge Changes**: Once the code has been reviewed and approved, the pull request can be merged into the main branch.

8. **Repeat Process**: The process starts again from step 2 for the next feature or bug fix.

To learn more, please refer to [GitHub Workflow Docs](https://docs.github.com/en/get-started/quickstart/github-flow).