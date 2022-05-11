""" Functions to assist with decoding text in DICOM files
"""

import os
import pydicom
import re
import random
from SmiServices.StructuredReport import sr_keys_to_extract, sr_keys_to_ignore


# ---------------------------------------------------------------------

def string_match(str1, str2):
    """ String comparison which ignores carriage returns \r by treating as spaces
    because that's how SemEHR new anonymiser delivers the string (in phi json anyway) """
    if re.sub('[\r\n]', ' ', str1) == re.sub('[\r\n]', ' ', str2):
        return True
    return False

def test_string_match():
    assert(string_match('hello', 'hello'))
    assert(string_match('hello\r\nworld', 'hello \nworld'))

# ---------------------------------------------------------------------

def redact_html_tags_in_string(html_str):
    """ Replace the HTML tags in a string with equal length of a
    repeating character (space or dot for example).
    Handles multi-line tags such as <style> and <script> which
    should have their enclosed text fully redacted.
    Doesn't handle embedded html within those sections though,
    so not truly robust but likely sufficient for simple html from
    clinical software.
    Returns the new string.
    """
    replchar = '.'
    def replfunc(s):
        return replchar.rjust(len(s.group(0)), replchar)
    # Use re.I (ignore case) re.M (multi-line) re.S (dot matches all)
    # re.S needed to match CR/LF when scripts are multi-line.
    # First replace single-instance tags <script.../> and <style.../>
    html_str = re.sub('<script[^>]*/>', replfunc, html_str)
    html_str = re.sub('<style[^>]*/>',  replfunc, html_str)
    # Now replace the whole <script>...</script> and style sequence
    html_str = re.sub('<script[^>]*>.*?</script>', replfunc, html_str, flags=re.I|re.M|re.S)
    html_str = re.sub('<style[^>]*>.*?</style>', replfunc, html_str, flags=re.I|re.M|re.S)
    # Finally remove single-instance tags like <p> and <br>
    html_str = re.sub('<(.DOCTYPE|a|abbr|acronym|address|applet|area|article|aside|audio|b|base|basefont|bdi|bdo|big|blockquote|body|br|button|canvas|caption|center|cite|code|col|colgroup|data|datalist|dd|del|details|dfn|dialog|dir|div|dl|dt|em|embed|fieldset|figcaption|figure|font|footer|form|frame|frameset|h1|h2|h3|h4|h5|h6|head|header|hr|html|i|iframe|img|input|ins|kbd|label|legend|li|link|main|map|mark|meta|meter|nav|noframes|noscript|object|ol|optgroup|option|output|p|param|picture|pre|progress|q|rp|rt|ruby|s|samp|script|section|select|small|source|span|strike|strong|style|sub|summary|sup|svg|table|tbody|td|template|textarea|tfoot|th|thead|time|title|tr|track|tt|u|ul|var|video|wbr)( [^<>]*){0,1}>', replfunc, html_str, flags=re.IGNORECASE)
    return(html_str)

def test_redact_html_tags_in_string():
    src = '<script src="s.js"/> <SCRIPT lang="js"> script1\n </script> text1 <1 month\r\n<BR>text2 <script> script2 </script> text3'
    dest = redact_html_tags_in_string(src)
    # changing the \r to a space in the expected string also tests the string_match function
    expected = '.................... ..................................... text1 <1 month \n....text2 .......................... text3'
    assert(string_match(dest, expected))

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

    Class variables determine whether unknown tags are included in the output
    (ideally this would be True but in practice we are only interested in known tags
    and SemEHR ignores them).
    Replace HTML entities such as <BR> with newline is not done, it might break redaction.
    Redaction replaces text with X; random length uses random number of X instead,
    but do not use random length unless you are sure the change in string length won't
    break something else (eg. the string is inside a file format where length matters,
    or we need to keep the SemEHR annotations around, with their char offsets, for other reasons).
    """
    _include_header = True           # SemEHR uses some header fields to give context to body
    _include_unexpected_tags = False # SemEHR does not use unknown tags anyway so ignore them
    _warn_unexpected_tag = False     # print if an unexpected tag is encountered
    _replace_HTML_entities = True    # replace HTML tags with same length of space chars
    _redact_random_length = False    # do not use True unless you're sure the change in length won't break something
    _redact_char = 'X'               # character used to redact text
    _redact_char_digit = '9'         # character used to redact digits in text

    def __init__(self, filename, \
        include_header = _include_header, \
        replace_HTML_entities = _replace_HTML_entities):
        """ The DICOM file is read during construction.
        """
        self._p_text = '' # maintain string progress during plaintext walk
        self._r_text = '' # maintain string progress during redaction walk
        self._redacted_text = ''
        self._redact_offset = 0
        self._offset_list = [] # XXX not used
        self._annotations = []
        self._filename = filename
        self._dicom_raw = pydicom.dcmread(filename)
        # Copy class settings to instance settings with overrides
        self._include_header = include_header
        self._replace_HTML_entities = replace_HTML_entities
        # XXX do we need to decode the text?
        self._dicom_raw.decode()

    def __repr__(self):
        return f'<DicomText: {self._filename}>'

    def SOPInstanceUID(self):
        """ Simply returns the SOPInstanceUID from the DICOM file
        in case you need to uniquely identify this input file.
        """
        return self._dicom_raw['SOPInstanceUID'].value

    def tag(self, tagname):
        """ Simply returns the value of the tag from the DICOM file.
        The tag is specified by name not by number:number.
        Returns the empty string if tag is not present.
        """
        tagcode = pydicom.datadict.tag_for_keyword(tagname)
        if tagcode in self._dicom_raw:
            return self._dicom_raw[tagcode].value
        else:
            return ''

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
            # Replace HTML tags with spaces
            if self._replace_HTML_entities:
                rc = redact_html_tags_in_string(rc)
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
        #  except explicitly do not include TextValue, handled below
        list_of_tagname_desired = [ k['tag'] for k in sr_keys_to_extract ]
        if self._include_header:
            for srkey in sr_keys_to_extract:
                if srkey['tag'] in self._dicom_raw and srkey['tag'] != 'TextValue':
                    line = '[[%s]] %s\n' % (srkey['label'], srkey['decode_func'](str(self._dicom_raw[srkey['tag']].value)))
                    self._p_text = self._p_text + line
        # Now read ALL tags and use a blacklist (and ignore already done in whitelist).
        # Private tags will have tagname='' so ignore those too.
        if self._include_header:
            for drkey in self._dicom_raw:
                tagname = pydicom.datadict.keyword_for_tag(drkey.tag)
                if not drkey.VR == 'SQ' and not tagname in sr_keys_to_ignore and not tagname in list_of_tagname_desired and not tagname == '':
                    if DicomText._include_unexpected_tags:
                        line = '[[%s]] %s\n' % (tagname, drkey.value)
                        self._p_text = self._p_text + line
                        if DicomText._warn_unexpected_tag:
                            print('Warning: including unexpected tag "%s" = "%s"' % (tagname, str(drkey.value)[0:20]))
                    else:
                        if DicomText._warn_unexpected_tag:
                            print('Warning: ignored unexpected tag "%s" = "%s"' % (tagname, str(drkey.value)[0:20]))
        # Now handle the TextValue tag
        # Wrap the text with [[Text]] and [[EndText]] for SemEHR
        if 'TextValue' in self._dicom_raw:
            self._p_text = self._p_text + '[[Text]]\n'
            self._p_text = self._p_text + str(self._dicom_raw['TextValue'].value + '\n')
            self._p_text = self._p_text + '[[EndText]]\n'
        # Now the text in the ContentSequence
        # Wrap the text with [[ContentSequence]] and [[EndContentSequence]] for SemEHR
        if 'ContentSequence' in self._dicom_raw:
            self._p_text = self._p_text + '[[ContentSequence]]\n'
            for content_sequence_item in self._dicom_raw.ContentSequence:
                content_sequence_item.walk(self._dataset_read_callback)
            self._p_text = self._p_text + '[[EndContentSequence]]\n'

    def redact_string(self, plaintext, offset, rlen, VR):
        """ Simple function to replace characters from the middle of a string.
        Starts at offset for rlen characters, replaced with X.
        If the VR (Dicom Value Representation) suggests a restricted
        character set then use a different redact_char (eg. numbers use '9')
        See https://dicom.nema.org/dicom/2013/output/chtml/part05/sect_6.2.html
        Can replace all for same length or randomise the amount.
        Returns the new string.
        """
        redact_length = rlen
        redact_char = DicomText._redact_char
        if VR in ['AS', 'CS', 'DA', 'DS', 'DT', 'IS', 'TM', 'UI']:
            redact_char = DicomText._redact_char_digit
        if DicomText._redact_random_length:
            redact_length = random.randint(-int(rlen/2), int(rlen/2))
        rc = plaintext[0:offset] + redact_char.rjust(redact_length, redact_char) + plaintext[offset+rlen:]
        return rc

    def _dataset_redact_callback(self, dataset, data_element):
        """ Internal function called during a walk of the dataset during redaction.
        Builds a class-member string _r_text as it goes.
        Uses the annotation list in self._annotations to redact text.
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
        #print('At %d = %s' % (current_start, str(data_element.value)))
        for annot in self._annotations:
            # Sometimes it reports text:None so ignore
            if not annot['text'] or (annot['start_char'] == annot['end_char']):
                annot['replaced'] = True
                continue
            # If already replaced then ignore
            if 'replaced' in annot:
                continue
            # Use the previously found offset to check if this annotation is within the current string
            if ((annot['start_char'] + self._redact_offset >= current_start-32) and
                    (annot['start_char'] + self._redact_offset < current_end+32)):
                annot_at = annot['start_char'] - current_start
                annot_end = annot['end_char'] - current_start
                replaced = False
                # SemEHR may have an extra line at the start so start_char offset need adjusting
                for offset in [self._redact_offset] + list(range(-32, 32)):
                    # Do the comparison using text without html but replace inside text with html
                    rc_without_html = redact_html_tags_in_string(rc) if self._replace_HTML_entities else rc
                    if string_match(rc_without_html[annot_at+offset : annot_end+offset], annot['text']):
                        replacement = self.redact_string(replacement, annot_at+offset, annot_end-annot_at, data_element.VR)
                        replaced = replacedAny = True
                        annot['replaced'] = True
                        #print('REPLACE: %s in %s at %d (offset %d)' % (repr(annot['text']), repr(replacement), annot_at, offset))
                        self._redact_offset = offset
                        break
                # Only need to report error at the end, no need for Warning here:
                #if not replaced:
                #    print('WARNING: offsets slipped:')
                #    print('  expected to find %s but found %s' % (repr(annot['text']), repr(rc[annot_at:annot_end])))
        if data_element.VR == 'PN' or data_element.VR == 'DA':
            # Always fully redact the content of PersonName and Date tags
            replacement = self.redact_string(rc, 0, len(rc), data_element.VR)
            replacedAny = True
        if replacedAny:
            data_element.value = replacement
        self._r_text = self._r_text + rc
        self._redacted_text = self._redacted_text + replacement
        return replacement if replacedAny else None


    def redact(self, annot_list):
        """ Redact the text in the DICOM using the annotation list
        which is a list of dicts { start_char, end_char, text }.
        Uses the annotation list and _p_text to find and redact text
        so parse must already have been called.
        Modifies the actual state of the DICOM dataset _dicom_raw.
        Returns False if not all redactions could be done.
        """
        assert(self._p_text) # you must have called parse first
        self._r_text = ''    # XXX could start with '\n' to match semehr behaviour
        self._redacted_text = ''
        self._annotations = annot_list
        if 'TextValue' in self._dicom_raw:
            self._dataset_redact_callback(None, self._dicom_raw['TextValue'])
        if 'ContentSequence' in self._dicom_raw:
            for content_sequence_item in self._dicom_raw.ContentSequence:
                content_sequence_item.walk(self._dataset_redact_callback)
        rc = True
        for annot in self._annotations:
            if not annot.get('replaced'):
                print('ERROR: could not find annotation (%s) in document' % repr(annot['text']))
                rc = False
        return rc

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
        if 'TextValue' in self._dicom_raw:
            dicom_dest.TextValue = self._dicom_raw.TextValue
        if 'ContentSequence' in self._dicom_raw:
            dicom_dest.ContentSequence = self._dicom_raw.ContentSequence
        dicom_dest.save_as(destfile)

def test_DicomText():
    """ The test function requires a specially-crafted DICOM file
    as provided with SRAnonTool that has been modified to include HTML.
    """
    dcm = os.path.join(os.path.dirname(__file__), '../../../applications/SRAnonTool/test/report10html.dcm')
    expected_without_header = """[[ContentSequence]]
# Request
MRI: Knee
# History
16 year old with right knee pain after an injury playing basketball.
# Findings
# Finding
......
..................................
..........
There is bruising of the medial femoral condyle with some intrasubstance injury to the medial collateral ligament. The lateral collateral ligament in intact. The Baker's  cruciate ligament is irregular and slightly lax suggesting a partial tear. It does not appear to be completely torn. The posterior cruciate ligament is intact. The suprapatellar tendons are normal.
# Finding
There is a tear of the posterior limb of the medial meniscus which communicates with the superior articular surface. The lateral meniscus is intact. There is a Baker's cyst and moderate joint effusion.
# Finding
Internal derangement of the right knee with marked injury and with partial tear of the ACL; there is a tear of the posterior limb of the medial meniscus. There is a Baker's Cyst and joint effusion and intrasubstance injury to the medial collateral ligament.
# Best illustration of finding
[[EndContentSequence]]
"""
    expected_without_header_with_html = """[[ContentSequence]]
# Request
MRI: Knee
# History
16 year old with right knee pain after an injury playing basketball.
# Findings
# Finding
<html>
<style>
P { color: red; }
</style>
<P><BR><P>
There is bruising of the medial femoral condyle with some intrasubstance injury to the medial collateral ligament. The lateral collateral ligament in intact. The Baker's  cruciate ligament is irregular and slightly lax suggesting a partial tear. It does not appear to be completely torn. The posterior cruciate ligament is intact. The suprapatellar tendons are normal.
# Finding
There is a tear of the posterior limb of the medial meniscus which communicates with the superior articular surface. The lateral meniscus is intact. There is a Baker's cyst and moderate joint effusion.
# Finding
Internal derangement of the right knee with marked injury and with partial tear of the ACL; there is a tear of the posterior limb of the medial meniscus. There is a Baker's Cyst and joint effusion and intrasubstance injury to the medial collateral ligament.
# Best illustration of finding
[[EndContentSequence]]
"""
    expected_with_header = """[[Study Description]] OFFIS Structured Reporting Samples
[[Study Date]] 
[[Series Description]] RSNA '95, Picker, MR
[[Content Date]] 20050530
[[Patient ID]] PIKR752962
[[Patient Name]] John R Walz
[[Patient Birth Date]] 19781024
[[Patient Sex]] M
[[Referring Physician Name]] 
[[ContentSequence]]
# Request
MRI: Knee
# History
16 year old with right knee pain after an injury playing basketball.
# Findings
# Finding
......
..................................
..........
There is bruising of the medial femoral condyle with some intrasubstance injury to the medial collateral ligament. The lateral collateral ligament in intact. The Baker's  cruciate ligament is irregular and slightly lax suggesting a partial tear. It does not appear to be completely torn. The posterior cruciate ligament is intact. The suprapatellar tendons are normal.
# Finding
There is a tear of the posterior limb of the medial meniscus which communicates with the superior articular surface. The lateral meniscus is intact. There is a Baker's cyst and moderate joint effusion.
# Finding
Internal derangement of the right knee with marked injury and with partial tear of the ACL; there is a tear of the posterior limb of the medial meniscus. There is a Baker's Cyst and joint effusion and intrasubstance injury to the medial collateral ligament.
# Best illustration of finding
[[EndContentSequence]]
"""

    # Parse with the normal header tags included
    dt = DicomText(dcm)
    dt.parse()
    assert(dt.text() == expected_with_header)

    # Parse without including the header tags
    dt = DicomText(dcm, include_header = False)
    dt.parse()
    assert(dt.text() == expected_without_header)

    # Parse without including the header tags
    dt = DicomText(dcm, include_header = False, replace_HTML_entities = False)
    dt.parse()
    assert(dt.text() == expected_without_header_with_html)
