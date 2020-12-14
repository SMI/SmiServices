""" Functions to assist with decoding text in DICOM files
"""

import pydicom
from .StructuredReport import sr_keys_to_extract, sr_keys_to_ignore


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

    """ Class variable determines whether unknown tags are included in the output
    (ideally this would be True but in practice we are only interested in known tags).
    Replace HTML entities such as <BR> with newline.
    Insert a [[Findings]] tag after the header tags so SemEHR can find the body of text.
    Redaction replaces text with X. Random length uses random number of X instead.
    """
    _include_unexpected_tags = False
    _replace_HTML_entities = False
    _insert_Findings_tag = False
    _redact_random_length = False
    _redact_char = 'X'

    def __init__(self, filename):
        """ The DICOM file is read during construction.
        """
        self._p_text = '' # maintain string progress during plaintext walk
        self._r_text = '' # maintain string progress during redaction walk
        self._redacted_text = ''
        self._offset_list = [] # XXX not used
        self._annotations = []
        self._filename = filename
        self._dicom_raw = pydicom.dcmread(filename)
        # XXX do we need to decode the text?
        self._dicom_raw.decode()

    def __repr__(self):
        return f'<DicomText: {self._filename}>'

    def SOPInstanceUID(self):
        """ Simply returns the SOPInstanceUID from the DICOM file
        in case you need to uniquely identify this input file.
        """
        return self._dicom_raw['SOPInstanceUID'].value

    def _dataset_read_callback(self, dataset, data_element):
        """ Internal function called during a walk of the dataset.
        Builds a class-member string _p_text as it goes.
        """
        rc = ''
        if data_element.VR in ['SH', 'CS', 'SQ', 'UI']:
            # "SH" Short String, "CS" Code String, "SQ" Sequence, "UI" UID ignored
            pass
        elif data_element.VR == 'LO':
            # "LO" Long String typically used for headings
            rc = rc + ('# %s' % str(data_element.value)) + '\n'
        else:
            rc = rc + ('%s' % (str(data_element.value))) + '\n'
            # XXX replace HTML entities? such as <BR> with newline
            if DicomText._replace_HTML_entities:
                rc = re.sub('<[Bb][Rr]>', '\n', rc)
        if rc == '':
            return
        self._offset_list.append( { 'offset':len(self._p_text), 'string': rc} )
        self._p_text = self._p_text + rc

    def parse(self):
        """ Walk the dataset to extract the text which can then be
        returned via the text() method.
        """
        self._p_text = ''
        # Start by enumerating all known desired tags (whitelist)
        list_of_tagname_desired = [ k['tag'] for k in sr_keys_to_extract ]
        for srkey in sr_keys_to_extract:
            if srkey['tag'] in self._dicom_raw:
                line = '[[%s]] %s\n' % (srkey['label'], srkey['decode_func'](str(self._dicom_raw[srkey['tag']].value)))
                self._p_text = self._p_text + line
        # Now read ALL tags and use a blacklist (and ignore already done in whitelist).
        # Private tags will have tagname='' so ignore those too.
        for drkey in self._dicom_raw:
            tagname = pydicom.datadict.keyword_for_tag(drkey.tag)
            if not drkey.VR == 'SQ' and not tagname in sr_keys_to_ignore and not tagname in list_of_tagname_desired and not tagname == '':
                print('Warning: unexpected tag "%s" = "%s"' % (tagname, str(drkey.value)[0:20]))
                if DicomText._include_unexpected_tags:
                    line = '[[%s]] %s\n' % (tagname, drkey.value)
                    self._p_text = self._p_text + line
        # Now for the main event, the text in the ContentSequence
        # XXX output [[Findings]] so that SemEHR knows where to start
        if DicomText._insert_Findings_tag:
            self._dataset_read_callback(None, DataElement(0x00100010, 'UT', 'Findings'))
        # The text appears as a set of strings inside this tag:
        if 'ContentSequence' in self._dicom_raw:
            self._p_text = self._p_text + '[[ContentSequence]]\n'
            for content_sequence_item in self._dicom_raw.ContentSequence:
                content_sequence_item.walk(self._dataset_read_callback)
            self._p_text = self._p_text + '[[EndContentSequence]]\n'

    def redact_string(self, plaintext, offset, len):
        """ Simple function to replace characters from the middle of a string.
        Starts at offset for len characters, replaced with X.
        Can replace all for same length or randomise the amount.
        Returns the new string.
        """
        redact_length = len
        if DicomText._redact_random_length:
            redact_length = random.randint(-int(len/2), int(len/2))
        rc = plaintext[0:offset] + DicomText._redact_char.rjust(redact_length, DicomText._redact_char) + plaintext[offset+len:]
        return rc

    def _dataset_redact_callback(self, dataset, data_element):
        """ Internal function called during a walk of the dataset during redaction.
        Builds a class-member string _r_text as it goes.
        Uses the annotation list to redact text.
        """

        rc = ''
        if data_element.VR in ['SH', 'CS', 'SQ', 'UI']:
            pass
        elif data_element.VR == 'LO':
            rc = rc + ('# %s' % str(data_element.value)) + '\n'
        else:
            rc = rc + ('%s' % (str(data_element.value))) + '\n'
        if rc == '':
            return
        # The current string is now len(self._r_text) ..to.. +len(rc)
        current_start = len(self._r_text)
        current_end   = current_start + len(rc)
        replacement = rc
        replacedAny = False
        for annot in self._annotations:
            if annot['start_char'] >= current_start and annot['start_char'] < current_end:
                annot_at = annot['start_char'] - current_start
                annot_end = annot['end_char'] - current_start
                replaced = replacedAny = False
                # SemEHR may have an extra LF at the start so start_char offset need adjusting
                for offset in [-1, 0, +1, -2, +2]:
                    if rc[annot_at+offset : annot_end+offset] == annot['text']:
                        replacement = self.redact_string(replacement, annot_at+offset, annot_end-annot_at+offset)
                        replaced = replacedAny = True
                        #print('REPLACE: %s at offset %d' % (annot['text'], offset))
                        break
                if not replaced:
                    print('WARNING: offsets slipped:')
                    print('  expected to find %s but found %s' % (annot['text'], rc[annot_at:annot_end]))
        if data_element.VR == 'PN' or data_element.VR == 'DA':
            # Always fully redact the content of a PersonName tag
            replacement = self.redact_string(rc, 0, len(rc))
            replacedAny = True
        if replacedAny:
            data_element.value = replacement         
        self._r_text = self._r_text + rc
        self._redacted_text = self._redacted_text + replacement


    def redact(self, annot_list):
        """ Redact the text in the DICOM using the annotation list
        which is a list of dicts { start_char, end_char, text }.
        Uses the annotation list and _p_text to find and redact text
        so parse must already have been called.
        Modifies the actual state of the DICOM dataset _dicom_raw.
        """
        assert(self._p_text) # you must have called parse first
        self._r_text = ''    # XXX could start with '\n' to match semehr behaviour
        self._redacted_text = ''
        self._annotations = annot_list
        if 'ContentSequence' in self._dicom_raw:
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
        if 'ContentSequence' in self._dicom_raw:
            dicom_dest.ContentSequence = self._dicom_raw.ContentSequence
            dicom_dest.save_as(destfile)

