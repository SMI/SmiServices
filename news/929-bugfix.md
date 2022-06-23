Structured Reports improvements from PR#929

* Updated documentation
* Simplify SRAnonTool using external program semehr_anon.py
* Handle ConceptNameCodeSequence which has VR but no Value
* Ensure 'replaced' flag is not reset
* Write replacement DICOM whichever content tag is found
* Extract metadata from Mongo to go alongside anonymised text
* Redact numeric DICOM tags with all '9' not all 'X'
* Allow badly-formatted text content which contains HTML but does not escape non-HTML symbols