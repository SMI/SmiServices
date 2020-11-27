""" Functions to assist with decoding DICOM files representing from JSON as Python dicts
"""

import collections
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

def tag_val(dicomdict, tagname):
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
    # The dcm2jsom or pydicom style has 'vr' and 'Value' keys
    # so extract the Value (also sometimes has vr but no Value).
    if isinstance(retval, collections.Mapping):
        if 'vr' in retval:
            val = retval.get('Value', '') # pydicom and dcm2json write Value
            if val == '':
                val = retval.get('val', '') # but I've also seen val
            retval = val
    # Single element list reduced to just the first element
    # but doing this breaks the assertions below.
    #if isinstance(retval, list) and len(retval)==1:
    #	retval = retval[0]
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

def sr_decode_plaintext(str):
    """ Decode a plain string of text.
    Could do any specific character encoding conversion, eg. to UTF-8
    but that's better done using pydicom's decode function.
    """
    return str

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
    assert isinstance(cncs, list)
    for cncs_item in cncs:
        if has_tag(cncs_item, 'CodeMeaning'):
            return tag_val(cncs_item, 'CodeMeaning')
    return ''

def test_sr_decode_ConceptNameCodeSequence():
    assert sr_decode_ConceptNameCodeSequence([]) == ''
    assert sr_decode_ConceptNameCodeSequence( [ { 'CodeMeaning': 'cm', 'CodeValue': 'cv' } ] ) == 'cm'


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
    return num_str+' '+units_str

def test_sr_decode_MeasuredValueSequence():
    assert sr_decode_MeasuredValueSequence(None) == ''
    assert sr_decode_MeasuredValueSequence( [ { 'NumericValue': '23', 'MeasurementUnitsCodeSequence': [ { 'CodeMeaning': 'cm', 'CodeValue': 'mm' } ] } ] ) == '23 mm'




# ---------------------------------------------------------------------


class DicomText:
    """ A class holding a DICOM file which can be parsed to extract the
    text, and can be redacted given a list of annotations.
    Typical usage:
    dicomtext = Dicom.DicomText(dcmname) # Reads the raw DICOM file
    dicomtext.parse()                    # Analyses the text inside the ContentSequence
    xmldictlist = Knowtator.annotation_xml_to_dict(xml.etree.ElementTree.parse(xmlfilename).getroot())
    dicomtext.redact(xmldictlist)        # Redacts the parsed text using the annotations
    dicomtext.write(redacted_dcmname)    # Writes out the redacted DICOM file
    OR
    write_redacted_text_into_dicom_file  # to rewrite a second file with redacted text
    """

    def __init__(self, filename):
        """ The DICOM file is read during construction.
        """
        self._p_text = '' # maintain string progress during plaintext walk
        self._r_text = '' # maintain string progress during redaction walk
        self._redacted_text = ''
        self._offset_list = [] # XXX not used
        self._annotations = []
        self._dicom_raw = pydicom.dcmread(filename)
        # XXX do we need to decode the text?
        self._dicom_raw.decode()


    def _dataset_read_callback(self, dataset, data_element):
        """ Internal function called during a walk of the dataset.
        Builds a class-member string _p_text as it goes.
        """
        rc = ''
        if data_element.VR in ['SH', 'CS', 'SQ']:
            pass
        elif data_element.VR == 'LO':
            rc = rc + ('[[%s]]' % str(data_element.value)) + '\n'
        else:
            rc = rc + ('%s' % (str(data_element.value))) + '\n'
        if rc == '':
            return
        self._offset_list.append( { 'offset':len(self._p_text), 'string': rc} )
        self._p_text = self._p_text + rc

    def parse(self):
        """ Walk the dataset to extract the text which can then be
        returned via the text() method.
        """
        self._p_text = ''
        for content_sequence_item in self._dicom_raw.ContentSequence:
            content_sequence_item.walk(self._dataset_read_callback)

    def redact_string(self, plaintext, offset, len):
        """ Simple function to replace characters from the middle of a string.
        Starts at offset for len characters, replaced with X.
        Can replace all for same length or randomise the amount.
        Returns the new string.
        """
        redact_char = 'X'
        redact_random_length = False

        redact_length = len
        if redact_random_length:
            redact_length = random.randint(-int(len/2), int(len/2))
        rc = plaintext[0:offset] + 'X'.rjust(redact_length,'X') + plaintext[offset+len:]
        return rc

    def _dataset_redact_callback(self, dataset, data_element):
        """ Internal function called during a walk of the dataset during redaction.
        Builds a class-member string _r_text as it goes.
        Uses the annotation list to redact text.
        Modifies the actual state of the DICOM dataset _dicom_raw.
        """

        rc = ''
        if data_element.VR in ['SH', 'CS', 'SQ']:
            pass
        elif data_element.VR == 'LO':
            rc = rc + ('[[%s]]' % str(data_element.value)) + '\n'
        else:
            rc = rc + ('%s' % (str(data_element.value))) + '\n'
        if rc == '':
            return
        # The current string is now len(self._r_text) ..to.. +len(rc)
        current_start = len(self._r_text)
        current_end   = current_start + len(rc)
        replacement = rc
        for annot in self._annotations:
            if annot['start_char'] >= current_start and annot['start_char'] < current_end:
                annot_at = annot['start_char'] - current_start
                annot_end = annot['end_char'] - current_start
                if rc[annot_at:annot_end+1] == annot['text']:
                    # text is found exactly where we expected so offsets are correct
                    replacement = self.redact_string(rc, annot_at, annot_end-annot_at+1)
                    data_element.value = replacement
                else:
                    # offsets have slipped, find proper offset and save adjustment
                    print('WARN: offsets slipped')
        self._r_text = self._r_text + rc
        self._redacted_text = self._redacted_text + replacement


    def redact(self, annot_list):
        """ Redact the text in the DICOM using the annotation list
        which is a list of dicts { start_char, end_char, text }.
        Uses the annotation list and _p_text to find and redact text
        so parse must already have been called.
        Modifies the actual state of the DICOM dataset _dicom_raw.
        """
        assert(self._p_text) # call parse first
        self._r_text = ''
        self._redacted_text = ''
        self._annotations = annot_list
        for content_sequence_item in self._dicom_raw.ContentSequence:
            content_sequence_item.walk(self._dataset_redact_callback)

    def text(self):
        """ Returns the text after parse() has been called.
        """
        return self._p_text

    def redacted_text(self):
        """ Returns the redacted text after redact() has been called.
        """
        return self._redacted_text

    def write(self, newfile):
        """ Save the (redacted) DICOM as a new file.
        """
        self._dicom_raw.save_as(newfile)

    def write_redacted_text_into_dicom_file(self, destfile):
        """ Open the specified file (must exist) and copy our redacted
        text into that file. The intention is that the redacted text
        from DICOM file A can be inserted into an already-anonymised
        DICOM file B.
        """
        dicom_dest = pydicom.dcmread(destfile)
        dicom_dest.ContentSequence = self._dicom_raw.ContentSequence
        dicom_dest.save_as(destfile)

