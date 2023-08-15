#!/usr/bin/env python3
# String utilities shared between DicomText.py and StructuredReport.py

import re

from html.parser import HTMLParser
#from html.entities import name2codepoint

class MyHTMLParser(HTMLParser):
    """ A HTML parser which simply extracts text and throws away tags,
    so you can get the text out.
    Character entities will be replaced with the actual character.
    No attempt to preserve original document length.
    Not sure how robust it is to badly formatted HTML.
    """
    def __init__(self):
        super().__init__()
        self._style_active = False
        self._script_active = False
        self._extracted_text = ''

    def extracted_text(self):
        return self._extracted_text

    def handle_starttag(self, tag, attrs):
        #print("Start tag:", tag)
        if tag == 'style':
            self._style_active = True
        if tag == 'script':
            self._script_active = True
        #for attr in attrs:
        #    print("     attr:", attr)
        return

    def handle_endtag(self, tag):
        #print("End tag  :", tag)
        if tag == 'style':
            self._style_active = False
        if tag == 'script':
            self._script_active = False
        return

    def handle_data(self, data):
        if not self._style_active and not self._script_active:
            data = data.strip()
            if len(data) > 0:
                #print("%s " % data)
                self._extracted_text += ("%s " % data)

    def handle_comment(self, data):
        #print("Comment  :", data)
        return

    def handle_entityref(self, name):
        # Never called since convert_charrefs=True
        c = chr(name2codepoint[name])
        #print("Named ent:", c)
        return

    def handle_charref(self, name):
        # Never called since convert_charrefs=True
        if name.startswith('x'):
            c = chr(int(name[1:], 16))
        else:
            c = chr(int(name))
        #print("Num ent  :", c)
        return

    def handle_decl(self, data):
        #print("Decl     :", data)
        return


# ---------------------------------------------------------------------

def string_match_ignore_linebreak(str1, str2):
    """ String comparison which ignores carriage returns \r by treating as spaces
    because that's how SemEHR new anonymiser delivers the string (in phi json anyway).
    This function is only used in DicomText._dataset_redact_callback() """
    if re.sub('[\r\n]', ' ', str1) == re.sub('[\r\n]', ' ', str2):
        return True
    return False

def test_string_match_ignore_linebreak():
    assert(string_match_ignore_linebreak('hello', 'hello'))
    assert(string_match_ignore_linebreak('hello\r\nworld', 'hello \nworld'))
    assert(string_match_ignore_linebreak('hello\r\nworld', 'hello  world'))
    assert(string_match_ignore_linebreak('hello\r\rworld', 'hello  world'))

# ---------------------------------------------------------------------

def redact_html_tags_in_string(html_str, replace_char='.', replace_newline='\n'):
    """ Replace the HTML tags in a string with equal length of a
    repeating character (space or dot for example).
    The character (or string!) is given in replace_char, default dot.
    You can also replace newlines with spaces using replace_newline=' '.
    Either can also be the empty string '' to squash the result.
    Handles multi-line tags such as <style> and <script> which
    should have their enclosed text fully redacted.
    Doesn't handle embedded html within those sections though,
    so not truly robust but likely sufficient for simple html from
    clinical software.
    Returns the new string.
    """
    replchar = replace_char
    def replfunc(s):
        return replchar.rjust(len(s.group(0)), replchar) if replchar else ''
    # First replace single-instance tags <script.../> and <style.../>
    html_str = re.sub('<script[^>]*/>', replfunc, html_str)
    html_str = re.sub('<style[^>]*/>',  replfunc, html_str)
    # Replace non-breaking space with multiple spaces to preserve string length
    html_str = re.sub('&nbsp;', '      ', html_str)
    # Replace both types of newlines
    html_str = re.sub('\r', replace_newline, html_str)
    html_str = re.sub('\n', replace_newline, html_str)
    # Now replace the whole <script>...</script> and style sequence.
    #  Use re.I (ignore case) re.M (multi-line) re.S (dot matches all)
    #  re.S needed to match CR/LF when scripts are multi-line.
    html_str = re.sub('<script[^>]*>.*?</script[^>]*>', replfunc, html_str, flags=re.I|re.M|re.S)
    html_str = re.sub('<style[^>]*>.*?</style[^>]*>', replfunc, html_str, flags=re.I|re.M|re.S)
    html_str = re.sub('<!--[^>]*-->', replfunc, html_str, flags=re.I|re.M|re.S) # does not handle comments containing html
    # Finally remove single-instance tags like <p> and <br>
    html_str = re.sub('</{0,1}(.DOCTYPE|a|abbr|acronym|address|applet|area|article|aside|audio|b|base|basefont|bdi|bdo|big|blockquote|body|br|button|canvas|caption|center|cite|code|col|colgroup|data|datalist|dd|del|details|dfn|dialog|dir|div|dl|dt|em|embed|fieldset|figcaption|figure|font|footer|form|frame|frameset|h1|h2|h3|h4|h5|h6|head|header|hr|html|i|iframe|img|input|ins|kbd|label|legend|li|link|main|map|mark|meta|meter|nav|noframes|noscript|object|ol|optgroup|option|output|p|param|picture|pre|progress|q|rp|rt|ruby|s|samp|script|section|select|small|source|span|strike|strong|style|sub|summary|sup|svg|table|tbody|td|template|textarea|tfoot|th|thead|time|title|tr|track|tt|u|ul|var|video|wbr)( [^<>]*){0,1}>', replfunc, html_str, flags=re.IGNORECASE)
    return(html_str)

def test_redact_html_tags_in_string():
    src = '<script src="s.js"/> <SCRIPT lang="js"> script1\n </script> text1 <1 month\r\n<BR>text2 <script> script2 </script> text3&nbsp;</p>'
    # changing the \r to a space in the expected string also tests the string_match_ignore_linebreak function
    dest = redact_html_tags_in_string(src)
    expected = '.................... ..................................... text1 <1 month \n....text2 .......................... text3      ....'
    assert(string_match_ignore_linebreak(dest, expected))
    # Test replacing HTML with spaces
    dest = redact_html_tags_in_string(src, replace_char=' ')
    expected = '                                                           text1 <1 month \n    text2                            text3          '
    assert(string_match_ignore_linebreak(dest, expected))
    # Test the newline replacement
    dest = redact_html_tags_in_string(src, replace_char=' ', replace_newline=' ')
    expected = '                                                           text1 <1 month      text2                            text3          '
    assert(string_match_ignore_linebreak(dest, expected))
    # Test squashing HTML and newlines
    dest = redact_html_tags_in_string(src, replace_char='', replace_newline='')
    expected = '  text1 <1 monthtext2  text3      '
    assert(string_match_ignore_linebreak(dest, expected))


def remove_html_tags_in_string(html_str):
    """ Remove all HTML tags from the string and return the new string.
    Does not try to preserve the original string length."""
    parser = MyHTMLParser()
    parser.feed(html_str)
    text_str = parser.extracted_text()
    return(text_str)

def test_remove_html_tags_in_string():
    dest = remove_html_tags_in_string('<!DOCTYPE fake> <style>stuff </style>hello <p>world')
    expected = 'hello world '
    assert(dest == expected)


# ---------------------------------------------------------------------

if __name__ == '__main__':
    import sys
    filename = sys.argv[1]
    with open(filename,  encoding='latin') as fd:
        doc = fd.read()
    rc = redact_html_tags_in_string(doc)
    print('REDACTED:')
    print(rc)
    rc = remove_html_tags_in_string(doc)
    print('REMOVED:')
    print(rc)
