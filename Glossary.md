# Glossary

## Dicom

DICOM (Digital Imaging and Communications in Medicine) is the international standard format for storing medical imaging. In the standard DICOM files contain both pixel data (the image) and [tag data].

## Dicom Tags

Images stored in the [Dicom] format include a metadata information (tags). Tags include information about the patient as well as information about the device used, scan settings, modality etc. Some tags are required for the file to be considered valid while others are optional. The format includes support for both tree data structures (Sequences) and arrays (Multiplicity).

Tags can be either part of the [standard set](https://dicom.innolitics.com/ciods) or invented by device manufacturers (private tags). Adherence to the standard varies widely (both by manufacturer, healthboard and over time).

## Loading

[Dicom] images can be very large but most of this space is taken up by the pixel data. Since it is the [tag data] that is most useful for generating cohorts it is these that are loaded.

'Loading' in the context of the SMI repository means extracting the dicom [tag data] and persisting it along with the relative file path of the image into a database.

Loading may also include typical data load operations e.g. summarization, error detect, anonymisation etc.

[dicom]: #dicom
[tag data]: #dicom-tags
