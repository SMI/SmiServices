# News files

This directory contains files describing changes since the previous release of SmiServices.

When a release is built, these files are automatically combined into the main [CHANGELOG](/CHANGELOG.md), and are then deleted.

## File naming

News file names should be of the form

```txt
<pr#>-<type>.md
```

e.g.

```txt
1234-feature.md
```

Where `type` is one of

-   `feature`
-   `change`
-   `bugfix`
-   `doc`
-   `removal`
-   `meta`

_Note_ Ensure that the file is named with the _PR_ number, rather than any associated _issue_ number.

Quick tip: You can get the most recent issue or PR number with the following one-liner. Then add one to determine the new one for your PR (so long as you're quick!)

```console
$ curl -s "https://api.github.com/repos/smi/smiservices/issues?sort=created&direction=desc&per_page=1&page=1" | jq .[].number
702
```

The file should contain a short description of the patch as one or more lines of markdown, either as a top-level sentence

```md
Fixed a foobar
```

or, if more detail is required, multiple lines formatted as a sub-list

```md
Fixed a foobar

-   Requires users to change xyz
```
