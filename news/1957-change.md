Refactor project path resolvers in CohortExtractor:

- `DefaultProjectPathResolver` is now `StudySeriesOriginalFilenameProjectPathResolver`
- Undo handling UIDs with leading dot (#1506) as this was causing difficulties with path lookups elsewhere
- Add `StudySeriesSOPProjectPathResolver` which generates filenames using SOPInstanceUID instead of the original file name. This is now the default path resolver
- Disallow null Study/Series/SOPInstanceUID values, which should never occur in practice
