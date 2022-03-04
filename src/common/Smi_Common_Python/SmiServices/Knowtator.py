# Convert from and to the Knowtator XML format
# for recording document annotations.
# This has been deduced from the SemEHR-anonymiser output
# and is intended only for working with that software.
# It has been extended to work like the ann_converter
# so the output can be used as input to eHOST.

from operator import itemgetter # for sorted()
import xml.etree.ElementTree    # untangle and xmltodict not available in NSH
import xml.dom.minidom as minidom
import re


# ---------------------------------------------------------------------
# Convert the knowtator.xml format into a Python dict
# by extracting the span start,end of all annotations.
# An example XML file:

# <?xml version='1.0' encoding='utf8'?>
# <annotations textSource="anon.txt">
#  <annotation>
#   <mention id="anon.txt-1"/>
#   <annotator id="semehr">semehr</annotator>
#   <span end="44" start="34"/>
#   <spannedText>16 year old</spannedText>
#   <creationDate>Wed November 11 13:04:51 2020</creationDate>
#  </annotation>
#  <classMention id="anon.txt-1">
#   <mentionClass id="semehr_sensitive_info">16 year old</mentionClass>
#  </classMention>
#  <annotation>
#   <mention id="anon.txt-1"/>
#   <annotator id="semehr">semehr</annotator>
#   <span end="726" start="722"/>
#   <spannedText>Baker</spannedText>
#   <creationDate>Wed November 11 13:04:51 2020</creationDate>
#  </annotation>
#  <classMention id="anon.txt-1">
#   <mentionClass id="semehr_sensitive_info">Baker</mentionClass>
#  </classMention>
#  <annotation>
#   <mention id="anon.txt-1"/>
#   <annotator id="semehr">semehr</annotator>
#   <span end="952" start="941"/>
#   <spannedText>Baker's Cyst</spannedText>
#   <creationDate>Wed November 11 13:04:51 2020</creationDate>
#  </annotation>
#  <classMention id="anon.txt-1">
#   <mentionClass id="Disease(C12345)">Baker's Cyst</mentionClass>
#  </classMention>
# </annotations>


def annotation_xml_to_dict(xmlroot):
    """ Convert from XML structure into python list of dicts sorted by start_char.
    Parameter: xmlroot should be the result of calling getroot() on an ElementTree.
    Structure should be annotations with span elements. classMentions are ignored.
    Returns: list of dicts { start_char, end_char, text, pref, cui }
    eg.
    xmlroot = xml.etree.ElementTree.parse(xmlfilename).getroot()
    xmldictlist = knowtator_xml_to_dict(xmlroot)
    """
    item_list = []
    for item in xmlroot:
        if item.tag == 'annotation':
            for prop in item:
                if prop.tag == 'span':
                    start = int(prop.attrib['start'])
                    end = int(prop.attrib['end'])
                elif prop.tag == 'spannedText':
                    txt = prop.text
            item_list.append({ 'start_char':start, 'end_char':end, 'text':txt })
        elif item.tag == 'classMention':
            # assuming order is always annotation followed by classMention
            # update the last annotation with this class, being "pref(cui)"
            for prop in item:
                if prop.tag == 'mentionClass' and '(' in prop.attrib['id']:
                    (pref, cui) = re.search(r'(.*)\((.*)\)', prop.attrib['id']).groups()
                    item_list.append( { 'pref': pref, 'cui': cui, **item_list.pop() } )
    return(sorted(item_list, key=itemgetter('start_char')))


def dict_to_annotation_xml_string(dictlist, anonymise=False, annotate=False):
    """ Convert a list of dict { start_char, end_char, text [,pref, cui] } into an XML string
    where pref and cui are optional and make up the class using "pref(cui)"
    if given otherwise the class "semehr_sensitive_info" is used.
    If using the output for correcting anonymisation set anonymise=True 
    so it uses class "semehr_sensitive_info".
    If using the output for correction annotation set annotate=True
    so it uses class "annotation_correct".
    To get the list of dict from a regex pattern in a string you could use:
    [{ 'start_char': m.start(), 'end_char': m.end(), 'text': pattern } for m in re.finditer(pattern, txt)]
    """
    xmlroot = xml.etree.ElementTree.Element('annotations')
    match_num=0
    for match in dictlist:
        match_num = match_num+1
        xmlitem = xml.etree.ElementTree.SubElement(xmlroot, 'annotation')
        xmlsubitem = xml.etree.ElementTree.SubElement(xmlitem, 'mention')
        xmlsubitem.set('id', f'filename-{match_num}')
        xmlsubitem = xml.etree.ElementTree.SubElement(xmlitem, 'annotator')
        xmlsubitem.set('id', f'filename-{match_num}')
        xmlsubitem.text = 'semehr'
        xmlsubitem = xml.etree.ElementTree.SubElement(xmlitem, 'span')
        xmlsubitem.set('start', str(match['start_char']))
        xmlsubitem.set('end', str(match['end_char']))
        xmlsubitem = xml.etree.ElementTree.SubElement(xmlitem, 'spannedText')
        xmlsubitem.text = match['text']
        xmlsubitem = xml.etree.ElementTree.SubElement(xmlitem, 'creationDate')
        xmlsubitem.text = 'Wed November 11 13:04:51 2020'
        xmlitem = xml.etree.ElementTree.SubElement(xmlroot, 'classMention')
        xmlitem.set('id', f'filename-{match_num}')
        xmlsubitem = xml.etree.ElementTree.SubElement(xmlitem, 'mentionClass')
        if anonymise:
            xmlsubitem.set('id', 'semehr_sensitive_info')
        elif annotate:
            xmlsubitem.set('id', 'annotation_correct')            
        elif 'pref' in match and 'cui' in match:
            xmlsubitem.set('id', '%s(%s)' % (match['pref'], match['cui']))
        else:
            xmlsubitem.set('id', 'semehr_sensitive_info')
        xmlsubitem.text = match['text']

    xmlstr = minidom.parseString(xml.etree.ElementTree.tostring(xmlroot)).toprettyxml(indent=" ")
    return xmlstr

def test_Knowtator():
    txml = """<?xml version="1.0" ?>
<annotations>
 <annotation>
  <mention id="filename-1"/>
  <annotator id="filename-1">semehr</annotator>
  <span end="7" start="5"/>
  <spannedText>stuff</spannedText>
  <creationDate>Wed November 11 13:04:51 2020</creationDate>
 </annotation>
 <classMention id="filename-1">
  <mentionClass id="semehr_sensitive_info">stuff</mentionClass>
 </classMention>
 <annotation>
  <mention id="filename-1"/>
  <annotator id="filename-1">semehr</annotator>
  <span end="17" start="15"/>
  <spannedText>nonsense</spannedText>
  <creationDate>Wed November 11 13:04:51 2020</creationDate>
 </annotation>
 <classMention id="filename-1">
  <mentionClass id="nothing(C000)">nonsense</mentionClass>
 </classMention>
</annotations>
"""
    #xmlroot = xml.etree.ElementTree.parse(xmlfilename).getroot()
    #xmldictlist = knowtator_xml_to_dict(xmlroot)
    # Test round-trip starting from a dict:
    adict = [
        { 'start_char': 5, 'end_char': 7, 'text': 'stuff' },
        { 'start_char': 15, 'end_char': 17, 'text': 'nonsense', 'pref': 'nothing', 'cui': 'C000' }
    ]
    axmlstr = dict_to_annotation_xml_string(adict)
    axmlroot = xml.etree.ElementTree.fromstring(axmlstr)
    adict2 = annotation_xml_to_dict(axmlroot)
    assert(adict == adict2)
    # Test starting from XML:
    axmlroot = xml.etree.ElementTree.fromstring(txml)
    adict3 = annotation_xml_to_dict(axmlroot)
    assert(adict == adict3)
