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
-   `misc`

The file should contain a short description of the patch as one or more lines of markdown, either as a top-level list element

```md
-   Fixed a foobar
```

or, if more detail is requried, multiple lines formatted as a sub-list

```md
-   Fixed a foobar
    -   Requires users to change xyz
```
