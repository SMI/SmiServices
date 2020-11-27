
from operator import itemgetter # for sorted()
import xml.etree.ElementTree    # untangle and xmltodict not available in NSH


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
#   <mentionClass id="semehr_sensitive_info">Baker's Cyst</mentionClass>
#  </classMention>
# </annotations>


def annotation_xml_to_dict(xmlroot):
    """ Convert from XML structure into python list of dicts sorted by start_char.
    Parameter: xmlroot should be the result of calling getroot() on an ElementTree.
    Structure should be annotations with span elements. classMentions are ignored.
    Returns: list of dicts { start_char, end_char, text }
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
            # XXX should check mentionClass id matches and is sensitive text
            pass
    return(sorted(item_list, key=itemgetter('start_char')))
