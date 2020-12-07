import collections
import re
import sys
from . import Dicom
from .Dicom import tag_is, tag_val, has_tag
import pydicom


# ---------------------------------------------------------------------
# List of known keys which we either parse or can safely ignore
# (all the others will be reported during testing to ensure no content is missed).
sr_keys_to_extract = [
    { 'label':'Study Description', 'tag':'StudyDescription', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Study Date', 'tag':'StudyDate', 'decode_func':Dicom.sr_decode_date },
    { 'label':'Series Description', 'tag':'SeriesDescription', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Series Date', 'tag':'SeriesDate', 'decode_func':Dicom.sr_decode_date },
    { 'label':'Performed Procedure Step Description', 'tag':'PerformedProcedureStepDescription', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'ProtocolName', 'tag':'ProtocolName', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'StudyComments', 'tag':'StudyComments', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Content Date', 'tag':'ContentDate', 'decode_func':Dicom.sr_decode_date },
    { 'label':'Patient ID', 'tag':'PatientID', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Patient Name', 'tag':'PatientName', 'decode_func':Dicom.sr_decode_PNAME },
    { 'label':'Patient Birth Date', 'tag':'PatientBirthDate', 'decode_func':Dicom.sr_decode_date },
    { 'label':'Patient Sex', 'tag':'PatientSex', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Patient Age', 'tag':'PatientAge', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Patient Weight', 'tag':'PatientWeight', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Medical Alerts', 'tag':'MedicalAlerts', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Allergies', 'tag':'Allergies', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Ethnic Group', 'tag':'EthnicGroup', 'decode_func':Dicom.sr_decode_plaintext },
    { 'label':'Referring Physician Name', 'tag':'ReferringPhysicianName', 'decode_func':Dicom.sr_decode_PNAME },
    { 'label':'Text', 'tag':'TextValue', 'decode_func':Dicom.sr_decode_plaintext },
]


sr_keys_to_ignore = [
    '_id',        # an artefact of the MongoDB extract
    'header',     # an artefact of the SMI MongoDB load microservice
    'InstanceCreationDate',
    'InstanceCreationTime',
    'SOPClassUID',
    'SOPInstanceUID',
    'StudyTime',
    'ContentTime',
    'AccessionNumber',
    'Modality',
    'ModalitiesInStudy',
    'Manufacturer',
    'InstitutionName',
    'ReferencedPerformedProcedureStepSequence',
    'TypeOfPatientID',
    'IssuerOfPatientID',
    'OtherPatientIDs',
    'OtherPatientIDsSequence',
    'StudyInstanceUID',
    'SeriesInstanceUID',
    'StudyID',
    'SeriesNumber',
    'InstanceNumber',
    'NumberOfStudyRelatedInstances',
    'ValueType',
    'ContinuityOfContent',
    'CurrentRequestedProcedureEvidenceSequence',
    'CompletionFlag',
    'VerificationFlag',
    'ContentTemplateSequence',
    'SpecificCharacterSet',
    'CodingSchemeIdentificationSequence',
    'ImageType',
    'SeriesTime',
    'InstitutionAddress',
    'StationName',
    'InstitutionalDepartmentName',
    'PhysiciansOfRecord',
    'ManufacturerModelName',
    'SoftwareVersions',
    'DateOfLastCalibration',
    'TimeOfLastCalibration',
    'RequestingPhysician',
    'RequestingService',              # XXX do we want this?
    'RequestedProcedureDescription',  # XXX do we want this?
    'RequestAttributesSequence',      # XXX what is this?
    'PerformedProcedureStepStartDate',
    'PerformedProcedureStepStartTime',
    'PerformedProcedureStepID',
    'PerformedProcedureCodeSequence', # XXX what is this? does it ever exist?
    'PerformedProtocolCodeSequence',  # XXX what is this?
    'ReferringPhysicianName',
    'ImageComments', # XXX do we want this?
    'TotalNumberOfExposures',
    'ExposedArea',
    'ExposedDoseSequence',
    'ExposureDoseSequence',
    'EntranceDoseInmGy',
    'ProcedureCodeSequence', # XXX what is this?
    'ReferencedStudySequence', # XXX what is this?
    'PatientSize', # XXX do we want this?
    'DeviceSerialNumber',
    'QueryRetrieveLevel',
    'ScheduledStepAttributesSequence',
    'AcquisitionDate',
    'AcquisitionTime',
    'AdditionalPatientHistory', # XXX do we want this?
    'AdmittingDiagnosesDescription', # XXX do we want this?
    'BodyPartExamined', # XXX do we want this?
    'BranchOfService',
    'CommentsOnThePerformedProcedureStep', # XXX do we want this?
    'CompletionFlagDescription',
    'ConfidentialityConstraintOnPatientDataDescription',
    'CountryOfResidence',
    'CountsAccumulated',
    'InstanceCreatorUID',
    'LastMenstrualDate',
    'MedicalRecordLocator',
    'MilitaryRank',
    'NameOfPhysiciansReadingStudy',
    'ObservationDateTime',
    'Occupation',
    'OperatorsName',
    'OtherPatientNames',
    'PatientAddress',
    'PatientBirthName',
    'PatientBirthTime',
    'PatientComments', # XXX do we want this?
    'PatientInsurancePlanCodeSequence',
    'PatientMotherBirthName',
    'PatientReligiousPreference',
    'PatientState',
    'PatientTelephoneNumbers',
    'PerformingPhysicianName',
    'PredecessorDocumentsSequence',
    'PregnancyStatus',
    'QualityControlImage',
    'ReferencedPatientSequence',
    'ReferencedRequestSequence',
    'RegionOfResidence',
    'RelationshipType',
    'RequestedProcedureCodeSequence', # XXX do we want this?
    'RequestedProcedureComments', # XXX do we want this?
    'ScheduledStudyStartDate',
    'ScheduledStudyStartTime',
    'SmokingStatus',
    'SpatialResolution',
    'SpecialNeeds',
    'StorageMediaFileSetUID',
    'StudyReadDate',
    'StudyReadTime',
    'PresentationIntentType',
    'ImagerPixelSpacing',
    'PositionerType',
    'DetectorType',
    'DetectorDescription',
    'DetectorID',
    'DetectorManufacturerName',
    'DetectorManufacturerModelName',
    'PatientOrientation',
    'ImageLaterality',
    'SamplesPerPixel',
    'PhotometricInterpretation',
    'Rows',
    'Columns',
    'PixelSpacing',
    'BitsAllocated',
    'BitsStored',
    'HighBit',
    'PixelRepresentation',
    'SmallestImagePixelValue',
    'LargestImagePixelValue',
    'BurnedInAnnotation',
    'PixelIntensityRelationship',
    'PixelIntensityRelationshipSign',
    'WindowCenter',
    'WindowWidth',
    'RescaleIntercept',
    'RescaleSlope',
    'RescaleType',
    'LossyImageCompression',
    'PresentationLUTShape',
    'OverlayRows',
    'OverlayColumns',
    'OverlayType',
    'OverlaySubtype',
    'OverlayOrigin',
    'OverlayBitsAllocated',
    'OverlayBitPosition',
    'OverlayData',
    'PixelData',
    'AcquisitionDateTime',
    'AcquisitionDeviceProcessingCode',
    'AcquisitionDeviceProcessingDescription',
    'AcquisitionNumber',
    'AnnotationDisplayFormatID',
    'BoneThermalIndex',
    'BorderDensity',
    'CineRate',
    'ConfidentialityCode',
    'ContrastBolusAgent',
    'ContrastBolusStartTime',
    'ConversionType',
    'DateOfLastDetectorCalibration',
    'DateOfSecondaryCapture',
    'DepthOfScanField',
    'DerivationDescription',
    'DetectorActiveShape',
    'DetectorBinning',
    'DetectorConfiguration',
    'DeviationIndex',
    'DistanceSourceToDetector',
    'DistanceSourceToPatient',
    'DocumentTitle',
    'EntranceDose',
    'Exposure',
    'ExposureControlMode',
    'ExposureIndex',
    'ExposureInuAs',
    'ExposureTime',
    'ExposureTimeInuS',
    'FieldOfViewDimensions',
    'FieldOfViewShape',
    'FilmOrientation',
    'FilterType',
    'FocalSpots',
    'FocusDepth',
    'FrameIncrementPointer',
    'FrameTime',
    'Grid',
    'HeartRate',
    'EncapsulatedDocument', # XXX is this only PDF, or ???
    'MIMETypeOfEncapsulatedDocument',
    'InterpretationStatusID',
    'ImageReference'        # XXX contains JSON, refers to other images, but also has PII
]


# ---------------------------------------------------------------------
# Return True if the DICOM tag named keystr can be ignored, either because
# it exists at the top level of the document and has already been output,
# or because it contains nothing of interest,
# or it is not recognised so cannot be decoded.
# Uses the global list sr_keys_to_ignore

def sr_key_can_be_ignored(keystr):
    if re.match('^[0-9a-fA-F]{8}$', keystr):
        keystr = pydicom.datadict.keyword_for_tag(keystr)
    if keystr in sr_keys_to_ignore:
        return True
    # If already handled explicitly elsewhere it can also be ignored
    for sr_extract_dict in sr_keys_to_extract:
        if keystr == sr_extract_dict['tag']:
            return True
    # We cannot definitively decode private tags, BUT some contain information which is not anywhere else, even person names!
    if '-PrivateCreator' in keystr:
        return True
    if '-Unknown' in keystr:
        return True
    if '-CSA ' in keystr: # part of SIEMENS CSA HEADER
        return True
    if ('-Dataset Name') in keystr: # part of GEMS_GENIE_1
        return True
    return False


# ---------------------------------------------------------------------
# Output the string given in valstr, if not empty.
# Prepends a section title given in keystr if not empty.
# eg.  [[keystr]] valstr
# Replaces HTML tags <BR> with newlines.
# Removes multiple line endings for clarity.

def _SR_output_string(keystr, valstr, fp):
    # If it's a list then print each element (but only expecting a single one line ['Findings'])
    if isinstance(valstr, list):
        return [_SR_output_string(keystr,X) for X in valstr]
    # The Key may also be a list but only take first element
    if isinstance(keystr, list):
        keystr = keystr[0]
    # If there is no value the do not print anything at all
    if valstr == None or valstr == '':
        return
    # Replace CRs with LF
    valstr = re.sub('\r+', '\n', valstr)
    # Replace HTML tags such as <br>
    valstr = re.sub('<[Bb][Rr]>', '\n', valstr)
    # Remove superfluous LFs
    valstr = re.sub('\n+', '\n', valstr)
    # If there is no key then do not print a prefix
    if keystr == None or keystr == '':
        fp.write('%s\n' % (valstr))
    else:
        fp.write('[[%s]] %s\n' % (keystr, valstr))


# ---------------------------------------------------------------------
# Internal function to parse a DICOM tag which calls itself recursively
# when it finds a sequence
# Uses str_output_string to format the output.

def _SR_parse_key(json_dict, json_key, fp):
        if tag_is(json_key, 'ConceptNameCodeSequence'):
            _SR_output_string('', Dicom.sr_decode_ConceptNameCodeSequence(tag_val(json_dict, json_key)), fp)
        elif tag_is(json_key, 'ContentSequence'):
            for cs_item in tag_val(json_dict, json_key):
                if has_tag(cs_item, 'RelationshipType') and has_tag(cs_item, 'ValueType'):
                    value_type = tag_val(cs_item, 'ValueType')
                    if value_type == 'PNAME' or value_type == ['PNAME']:
                        _SR_output_string(Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptNameCodeSequence')), tag_val(cs_item, 'PersonName'), fp)
                    elif value_type == 'DATETIME' or value_type == ['DATETIME']:
                        _SR_output_string(Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptNameCodeSequence')), tag_val(cs_item, 'DateTime'), fp)
                    elif value_type == 'DATE' or value_type == ['DATE']:
                        _SR_output_string(Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptNameCodeSequence')), tag_val(cs_item, 'Date'), fp)
                    elif value_type == 'TEXT' or value_type == ['TEXT']:
                        _SR_output_string(Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptNameCodeSequence')), tag_val(cs_item, 'TextValue'), fp)
                    elif (value_type == 'NUM' or value_type == ['NUM']) and has_tag(cs_item, 'MeasuredValueSequence'):
                        _SR_output_string(Dicom.sr_decode_ConceptNameCodeSequence(cs_item['ConceptNameCodeSequence']), Dicom.sr_decode_MeasuredValueSequence(tag_val(cs_item, 'MeasuredValueSequence')), fp)
                    elif (value_type == 'NUM' or value_type == ['NUM']) and has_tag(cs_item, 'NumericValue'):
                        _SR_output_string(Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptNameCodeSequence')), tag_val(cs_item, 'NumericValue'), fp)
                    elif value_type == 'CODE' or value_type == ['CODE']:
                        _SR_output_string(Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptNameCodeSequence')), Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptCodeSequence')), fp)
                    elif value_type == 'UIDREF' or value_type == ['UIDREF']:
                        _SR_output_string(Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptNameCodeSequence')), tag_val(cs_item, 'UID'), fp)
                    elif value_type == 'IMAGE' or value_type == ['IMAGE']:
                        _SR_output_string(Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptNameCodeSequence')), Dicom.sr_decode_ReferencedSOPSequence(tag_val(cs_item, 'ReferencedSOPSequence')), fp)
                    elif value_type == 'CONTAINER' or value_type == ['CONTAINER']:
                        # Sometimes it has no ContentSequence or is 'null'
                        if has_tag(cs_item, 'ContentSequence') and tag_val(cs_item, 'ContentSequence') != None:
                            _SR_output_string('', Dicom.sr_decode_ConceptNameCodeSequence(tag_val(cs_item, 'ConceptNameCodeSequence')), fp)
                            _SR_parse_key(cs_item, 'ContentSequence', fp)
                    else:
                        print('UNEXPECTED ITEM OF TYPE %s = %s' % (value_type, cs_item), file=sys.stderr)
                #print('ITEM %s' % cs_item)
        else:
            if not sr_key_can_be_ignored(json_key):
                print('UNEXPECTED KEY %s' % json_key, file=sys.stderr)
                print(json_dict[json_key])


# ---------------------------------------------------------------------
# Main function to parse a DICOM Structured Report in JSON format as
# output by the MongoDB database.

def SR_parse(json_dict, doc_name, fp = sys.stdout):

    _SR_output_string('Document name', doc_name, fp)

    # Output a set of known tags from the root of the document
    # This loop does the equivalent of
    # _SR_output_string('Study Date', sr_decode_date(sr_get_key(json_dict, 'StudyDate')))
    for sr_extract_dict in sr_keys_to_extract:
        _SR_output_string(sr_extract_dict['label'], sr_extract_dict['decode_func'](Dicom.tag_val(json_dict, sr_extract_dict['tag'])), fp)

    # Now output all the remaining tags which are not ignored
    for json_key in json_dict:
        _SR_parse_key(json_dict, json_key, fp)
