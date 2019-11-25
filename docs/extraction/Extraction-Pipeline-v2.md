
# Extraction Pipeline v2 Plan

Below is with reference to the updated [extraction diagram](Extraction-Pipeline-v2.png)

## Features / Requirements

1. Integrate the anonymisation validation into the pipeline
  - Due to the (potential) high volume of anonymised files to review, the IsIdentifable tool should be incorporated into a microservice that we can run multiple instances of. This should be designed in such a way that additional "checker" components can easily be added/configured if needed
  - Both tags and pixel data should be checked(?)
  - The IsIdentifiable tool is overly sensitive, so there will be many false positives which need to be manually reviewed and given a final pass/fail before the extraction is completed. In order to make this review process easier, we could create a small CLI script which grabs all the required info from CohortPackager (filename, fail reason, ...) and output it to a single file which the reviewer can mark-up with their own pass/fail (+ reason). The tool would then read this file back and pass it to CohortPackager to complete the extraction job
  - Any files which fail the (manual) validation check should be blacklisted in the cohort DB for future. This will likely be a manual task for now

2. (BI#25) Extraction pipeline needs to output a summary report
  - CohortExtractor needs to include information in the ExtractFileCollectionInfoMessage on files which were not extractable
  - CohortPackager needs to handle the above
  - Once the manual review has been completed, the final report should be generated and placed in the final output directory(?)

3. CohortPackager refactoring
  - This service struggled with the volume of messages when extracting by ImageID due to excessive use of locks around the extraction collection. We can refactor this to use separate consumers and collections so that consumers which have a high message rate don't have to wait for any locks before writing. We can also switch to doing bulk writes  
  - We should only copy the anonymised files to the copy-out directory once the manual review has been completed and the report produced to avoid any accidental copying to the researcher area. We can then create the COPY trigger file automatically(?)
  - (Low Prio.) Send notifications to Mattermost (extract completed / errored / ready for review etc.)

4. Auditing
  - Track/audit changes to the CTP script(?)
  - Track/audit changes to IsIdentifiable(?)
  - MongoDB extraction database needs included in the cron backup script

## Backlog Items / Tasks

TODO:
- Create further tasks from the requirements above

- 4 - Run a full happy-path extraction
- 24 - Documentation for extraction process (SMI team & eDRIS)
- 26 - Add a "Strict" mode to IsIdentifiable to run on extraction outputs after CTP
- 27 - Extraction process should use a hierarchical folder structure
- 32 - Create the "extractable" flags in the image supertable (see spec <here>)
- 36 - Verify ALL private data is removed by CTP (see comment)
- \* 42 - Extraction to support StudyInstanceUID + Modality
  - Do we need this for the MVP?
- 43 - Create the image supertable
- 50 - Update the copy tool script
- Check files are outputted to `<EUPI>/<StudyID>/<SeriesID>/<files>.dcm` as standard (with the extension if possible)

## Questions

- (RKM) CohortPackager is doing a lot of work here - should we split it into multiple components?
- (RKM) If we're applying the same CTP script to every file, can we create a cache of previously-anonymised files?
- (RKM) Do we need to also include a manual check of a random sample of anonymised files?
