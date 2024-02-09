""" Functions to assist with decoding DICOM files representing from JSON as Python dicts
"""

from collections.abc import Mapping
import re
import pydicom


# Hex string (without 0x prefix) for the tag number
DICOM_ContentSequence_TagString = '0040A730'


# ---------------------------------------------------------------------
# Functions to test/extract the values of DICOM tags
# eg.
# tag_alt swaps between hex and text tag names
#  "00100010" <-> "PatientName"
# tag_val returns the value of a given tag, can request
#  a hex number or string name, will return the value,
#  and if it's a dict with 'vr' (data type) and 'Value'
#  keys then it returns the Value.
# has_tag simply tests if that tag is in the dict,
#  again can pass a hex or name of the tag.
# tag_is tests if the two tag names are the same,
#  but the first can be a hex string which is converted
#  into a name string before being compared.

def tag_alt(tag):
    """ Return the alternative representation for a tag name
    so 8-digit uppercase hex string or a name string.
    """
    if re.match('^[0-9a-fA-F]{8}$', tag):
        return pydicom.datadict.keyword_for_tag(tag)
    # Need to ensure it's 8 digits with leading zeros
    alt = '{:0>8X}'.format(pydicom.datadict.tag_for_keyword(tag))
    return(alt)

def tag_val(dicomdict, tagname, atomic = False):
    """ Look up dicomdict['tagname']
      where tagname can be a hex string or a name string
      and the dicomdict can hold either the hex string or the name
      Returns the value of the tag, but if the tag contains
      keys called 'vr' and 'Value' then returns the value of 'Value'
    """
    retval = None
    alt_tagname = tag_alt(tagname)
    if tagname in dicomdict:
        retval = dicomdict[tagname]
    elif alt_tagname in dicomdict:
        retval = dicomdict[alt_tagname]
    # The dcm2json or pydicom style has 'vr' and 'Value' keys
    # so extract the Value (also sometimes has vr but no Value).
    if isinstance(retval, Mapping):
        if 'vr' in retval:
            val = retval.get('Value', '') # pydicom and dcm2json write Value
            if val == '':
                val = retval.get('val', '') # but I've also seen val
            retval = val
    # Single element list reduced to just the first element
    # only if you explicitly request this with atomic=True.
    if isinstance(retval, list) and len(retval)==1 and atomic:
    	retval = retval[0]
    return(retval)

def tag_is(tagA, tagB):
    """ Test if tagA is the same as tagB
    where tags can be a number or a name.
    Tries converting tagA from number to name too"""
    # XXX only converts the first one
    if re.match('^[0-9a-fA-F]{8}$', tagA):
        tagA = pydicom.datadict.keyword_for_tag(tagA)
    return(tagA == tagB)

def has_tag(dicomdict, tagname):
    """ Test is tagname is in the dict,
    where tagname can be a number or a name"""
    if tagname in dicomdict:
        return True
    alt_tagname = tag_alt(tagname)
    if alt_tagname in dicomdict:
        return True
    return False


# ---------------------------------------------------------------------
# Decode a plain string. Could be useful if the encoding was read from
# the DICOM file and used within this function.
# XXX Not yet implemented.

def sr_decode_plaintext(pstr):
    """ Decode a plain string of text.
    Could do any specific character encoding conversion, eg. to UTF-8
    but that's better done using pydicom's decode function.
    """
    return pstr

def test_sr_decode_plaintext():
    assert sr_decode_plaintext('hello world') == 'hello world'


# ---------------------------------------------------------------------
# Decode a DICOM date tag into a readable string

def sr_decode_date(datestr):
    """ Decode a string in DICOM's date format.
    XXX TODO: parse YYYYMMDD and return YYYY-MM-DD ?
    """
    return datestr

def test_decode_date():
    assert sr_decode_date('20200101') == '20200101'


# ---------------------------------------------------------------------
# Decode the string value of the PNAME tag.
# Decode the Person's Name value which is typically encoded
# using ^ as a separator between Surname^First^Middle^Title^Suffix
# Returns the decoded human-readable string.

def sr_decode_PNAME(pname):
    if pname == None:
        return ''
    # Can be inside a list, but only use the first element XXX
    if isinstance(pname, list):
        pname = pname[0]
    # Can be inside a dict { "Alphabetic" : "the name" }
    if isinstance(pname, dict) and 'Alphabetic' in pname:
        pname = pname['Alphabetic']
    if '^' in pname:
        pname_list = pname.split('^')
        pname = ''
        for ii in (3, 1, 2, 0, 4):
            if (len(pname_list) > ii) and (len(pname_list[ii]) > 0):
                pname = pname + pname_list[ii] + ' '
        pname = pname.rstrip()  # remove trailing spaces
    return pname


def test_sr_decode_PNAME():
    assert sr_decode_PNAME('Fukuda^Katherine M.^^^M. D.') == 'Katherine M. Fukuda M. D.'
    assert sr_decode_PNAME({ "Alphabetic" : 'Fukuda^Katherine M.^^^M. D.' }) == 'Katherine M. Fukuda M. D.'


# ---------------------------------------------------------------------
# 

def sr_decode_ReferencedSOPSequence(rss):
    assert isinstance(rss, list)
    for rss_item in rss:
        if has_tag(rss_item, 'ReferencedSOPInstanceUID'):
            return tag_val(rss_item, 'ReferencedSOPInstanceUID')
    return ''

def test_sr_decode_ReferencedSOPSequence():
    assert sr_decode_ReferencedSOPSequence([]) == ''
    assert sr_decode_ReferencedSOPSequence( [ { 'ReferencedSOPInstanceUID' : 'rsiuid' } ] ) == 'rsiuid'


# ---------------------------------------------------------------------
# Decode the ConceptNameCodeSequence by returning the value of CodeMeaning inside

def sr_decode_ConceptNameCodeSequence(cncs):
    if not cncs:
        return ''
    assert isinstance(cncs, list)
    for cncs_item in cncs:
        if has_tag(cncs_item, 'CodeMeaning'):
            return tag_val(cncs_item, 'CodeMeaning')
    return ''

def test_sr_decode_ConceptNameCodeSequence():
    assert sr_decode_ConceptNameCodeSequence([]) == ''
    assert sr_decode_ConceptNameCodeSequence( [ { 'CodeMeaning': 'cm', 'CodeValue': 'cv' } ] ) == 'cm'


# ---------------------------------------------------------------------
# Decode a SourceImageSequence by returning a string consisting of the
# UID of the referenced image.
# XXX ignores the ReferencedSOPClassUID.
# XXX this is the same as ReferencedSOPSequence above.

def sr_decode_SourceImageSequence(sis):
    assert isinstance(sis, list)
    for sis_item in sis:
        if has_tag(sis_item, 'ReferencedSOPInstanceUID'):
            return tag_val(sis_item, 'ReferencedSOPInstanceUID')
    return ''

def test_sr_decode_SourceImageSequence():
    assert sr_decode_SourceImageSequence([]) == ''
    assert sr_decode_SourceImageSequence( [ {'ReferencedSOPClassUID':'1.2', 'ReferencedSOPInstanceUID':'1.2.3.4.5'} ] ) == '1.2.3.4.5'


# ---------------------------------------------------------------------
# Decode a MeasuredValueSequence by returning a string consisting of the
# NumericValue inside, and having the short form of the units appended
# eg. 23mm.
# XXX could insert a space before the units if required

def sr_decode_MeasurementUnitsCodeSequence(mucs):
    assert isinstance(mucs, list)
    for mucs_item in mucs:
        # NB CodeValue pulls out the abbreviation, eg. 'mm',
        # use CodeMeaning if you want the full name, eg. 'millimeter'
        if has_tag(mucs_item, 'CodeValue'):
            return tag_val(mucs_item, 'CodeValue')
    return ''

def test_sr_decode_MeasurementUnitsCodeSequence():
    assert sr_decode_MeasurementUnitsCodeSequence([]) == ''
    assert sr_decode_MeasurementUnitsCodeSequence( [ { 'CodeMeaning': 'cm', 'CodeValue': 'cv' } ] ) == 'cv'


def sr_decode_MeasuredValueSequence(mvs):
    # Sometimes it is 'null' when no measurement was attempted
    # A NumericValueQualifierCodeSequence would report this but we don't check for it.
    if mvs == None:
        return ''
    assert isinstance(mvs, list)
    num_str = ''
    units_str = ''
    for mvs_item in mvs:
        if has_tag(mvs_item, 'NumericValue'):
            num_str = tag_val(mvs_item, 'NumericValue')
        if has_tag(mvs_item, 'MeasurementUnitsCodeSequence'):
            units_str = sr_decode_MeasurementUnitsCodeSequence(tag_val(mvs_item, 'MeasurementUnitsCodeSequence'))
    # Have to use str() because sometimes the value is missing, i.e. None(!)
    return str(num_str)+' '+str(units_str)

def test_sr_decode_MeasuredValueSequence():
    assert sr_decode_MeasuredValueSequence(None) == ''
    assert sr_decode_MeasuredValueSequence( [ { 'NumericValue': None, 'MeasurementUnitsCodeSequence': [ { 'CodeMeaning': 'cm', 'CodeValue': 'mm' } ] } ] ) == 'None mm'
    assert sr_decode_MeasuredValueSequence( [ { 'NumericValue': { 'vr':'DS' }, 'MeasurementUnitsCodeSequence': [ { 'CodeMeaning': 'cm', 'CodeValue': 'mm' } ] } ] ) == ' mm'
    assert sr_decode_MeasuredValueSequence( [ { 'NumericValue': { 'vr':'DS', 'Value': '23' }, 'MeasurementUnitsCodeSequence': [ { 'CodeMeaning': 'cm', 'CodeValue': 'mm' } ] } ] ) == '23 mm'
    assert sr_decode_MeasuredValueSequence( [ { 'NumericValue': '23', 'MeasurementUnitsCodeSequence': [ { 'CodeMeaning': 'cm', 'CodeValue': 'mm' } ] } ] ) == '23 mm'



# ---------------------------------------------------------------------
