Note: This was copied from a rough spec. document which was last updated on 2018-11-29.

# Image Extraction Flags & Refactoring

**Version 2.0 - 2018-11-29**

In the initial dataset we published to, only CT images with ImageType like `ORIGINAL\PRIMARY` were copied from our internal catalogue. This was our initial method for controlling what images were valid for extraction. It is important however that researchers can be informed of the data that will be included (and excluded) from any cohort that is generated, and also that we can disable certain images from being extracted due to 'dodgy' or corrupt data.

This document describes the proposed changes to our catalogue schemas and extraction process to allow for it. It also includes changes and bug-fixes which were discovered during testing.

## Metadata catalogue changes

### Image file table

We have discussed this when dealing with managing separate tables for each modality, but the general idea is to have a single table containing all the dicom file paths and any other information that is related to the specific file rather than any piece of metadata. We can use this here to store a flag for specific images we have deemed "not extractable". The schema might look something like:

```
SOPInstanceUID 			- VARCHAR(64) NOT NULL PRIMARY KEY
RelativeFileArchiveURI	- VARCHAR(512) NOT NULL
ExtractableFlag 		- BIT DEFAULT TRUE
ExtractableReason 		- TEXT DEFAULT NULL
```

In the case where an image is loaded through the extraction pipeline again after previously being marked not extractable, the flag should remain unset and not be reset to the default. This won't be needed if we also mark the image as not extractable in MongoDB in some way.

To aid the RC team, we could also add an Extractable flag at the Series/Study level which would indicate that every image in series / series in study has been 'disabled'. This could be automatically generated as part of a stored procedure, or when image(s) are manually disabled.

### Extractable white-list rules table

This is the other part of our definition what is extractable. This would consist of a very simple table containing rules that can be used to determine if an image is allowed to be extracted. An image would be considered extractable if the result of applying all the rules in a row to its dicom tags is true, for any row. For our current rules, we would have something like:

| ImageType                  | Modality |
| -------------------------- | -------- |
| LIKE '%ORIGINAL\\PRIMARY%' | EQ 'CT'  |
| LIKE '%ORIGINAL\\PRIMARY%' | EQ 'MR'  |

Exact syntax of this to be decided. For example, any given image would be extractable if matched the filter `ImageType LIKE '%ORIGINAL\\PRIMARY%' AND Modality = 'CT'` or `ImageType LIKE '%ORIGINAL\\PRIMARY%' AND Modality = 'MR'`.

-   ❓ How would this evolve over time? (performance for many rules, ordering of rules etc.)
-   ❓ This should be fairly easy to deal with in the application layer, but how easy will it be calculate if an image is extractable from a database query?
-   ❓ How to manage adding new columns to this?

## Extraction changes

Majority of this is software-related, however we should really create a short extraction guide for the RCs to use. It should include:

-   Valid identifier formats permitted in the ID request file (i.e. "SOPInstanceUID" not "imageId")
-   Notes on use of extractable flags

### CohortExtractor

At extraction, the CohortExtractor will start and load the table of white-list rules. It will then query the catalogues as normal and return a set of file paths matching the identifier(s) given in the message. It will then apply the extractable rules to the returned data; firstly checking if the ExtractableFlag is set, then applying the rules from the loaded white-list. It will then emit messages for any extractable images as normal, but also emit audit messages for anything not extractable, which will be recorded by the CohortPackager. This will require some messages to be updated.

As an aside, we would also like to refactor our output directory format to match `<ExtractRoot>/<EUPI>/<StudyInstanceUID>/<SeriesInstanceUID>/*.dcm` as standard. This means we need 2 dicom UIDs no matter which one we are using as the key for extraction.

This would mean we need to join across 3 tables to get all the information required for extracting an image (Study/Series/SOPInstanceUID, FilePath, Extractable info).

### CohortPackager

The CohortPackager now needs to handle the messages where we don't expect an anonymised image to be produced. We should be able to provide a summary of number of images requested/extractable/extracted and generate some sort of report of not extracted / reason / counts. For auditing, we lso need to be able to determine which extractions contain a specific image if requested.
