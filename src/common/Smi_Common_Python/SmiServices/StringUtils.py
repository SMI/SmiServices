#!/usr/bin/env python3
# String utilities shared between DicomText.py and StructuredReport.py

import re

from html.parser import HTMLParser
from html.entities import name2codepoint

# ---------------------------------------------------------------------

class RedactingHTMLParser(HTMLParser):

    def __init__(self, replace_char = ' '):
        super().__init__(convert_charrefs = False)
        self.replace_char = replace_char # HTML tags replaced with
        self.style_active = False    # is processing a <style>
        self.script_active = False   # is processing a <script>
        self.data_active = False     # is processing real text content
        self.newline_active = False  # is processing newlines
        self.ref_char = 0            # is an entity or character reference
        self.html_str = None         # redacted document
        self.rc = []                 # redaction offsets
        self.curpos = (0,0)          # last HTML entity offset
        # Change empty string to a nul character so it can be removed at the end
        if not self.replace_char:
            self.replace_char = '\0'

    def feed(self, html_str):
        # Build a list of line numbers and their character positions
        idx = 0
        eol_is_next = 0
        linenum = 1               # first line is 1, not zero
        self.linepos = [0]
        self.linepos.insert(1, 0) # first line is 1, not zero
        for ch in html_str:
            if ch == '\n' or ch == '\r':
                # If prev newline char was same then is a new line
                # otherwise CR,LF is a single newline
                if eol_is_next and (eol_is_next == ch):
                    linenum += 1
                    self.linepos.insert(linenum, idx)
                eol_is_next = ch
                idx += 1
                continue
            if eol_is_next:
                linenum += 1
                self.linepos.insert(linenum, idx)
                eol_is_next = 0
            idx += 1
        # At EOF add a fake final line because getpos() will return it
        self.linepos.insert(linenum+1, idx)
        self.html_str = html_str
        super().feed(html_str)

    def result(self):
        """ Return the redacted HTML string.
        Could also return the array of character offsets to be redacted.
        """
        # Ensure the final item is processed
        self.handle_prev()
        # If user wants to remove HTML tags (not redact) do this now
        self.html_str = self.html_str.replace('\0', '')
        return self.html_str

    def handle_prev(self):
        """ Output or redact the previous item which is between the
        character offsets  self.curpos through to self.getpos().
        """
        add_lf = self.newline_active
        self.newline_active = False
        if self.getpos() == (1,0):
            # Called at very start of document, no previous elements to redact
            return
        if not self.data_active or (self.style_active or self.script_active or self.ref_char):
            startline = self.curpos[0]
            startoffset = self.linepos[startline] + self.curpos[1]
            endline, endchar = self.getpos()
            endoffset = self.linepos[endline] + endchar
            redact_char = self.replace_char
            redact_length = endoffset - startoffset
            # First char needs to be replaced with newline?
            if add_lf:
                redact_str = '\n'
            else:
                redact_str = redact_char
            # Rest of replacement string is repeated chars
            redact_str = redact_char.rjust(redact_length, redact_char)
            # First char needs to be replaced with newline?
            if add_lf:
                redact_str = '\n' + redact_str[1:]
                redact_str = redact_str[:-1] + '\n'
            # Entity/char reference replaces first character in the replacement string
            if self.ref_char:
                redact_str = self.ref_char + redact_str[1:]
            # Reset flag for next time
            self.ref_char = 0
            # Rebuild string by cutting out redacted section and replacing it
            self.html_str = self.html_str[:startoffset] + redact_str + self.html_str[endoffset:]
            # Create array of offsets which might be returned
            self.rc.append( (startoffset, endoffset) )

    def prepare_next(self, data_active):
        self.data_active = data_active
        self.curpos = self.getpos()

    def handle_starttag(self, tag, attrs):
        self.handle_prev()
        # Ensure that any text is not output whilst in <style> or <script>
        if tag == 'style':
            self.style_active = True
        if tag == 'script':
            self.script_active = True
        # <p> and <br> cause a newline to be output
        if tag == 'p' or tag == 'br':
            self.newline_active = True
        # Prepare for next item
        self.prepare_next(False)
        return

    def handle_endtag(self, tag):
        self.handle_prev()
        # Remember for the next tag
        if tag == 'style':
            self.style_active = False
        if tag == 'script':
            self.script_active = False
        # Prepare for next item
        self.prepare_next(False)
        return

    def handle_startendtag(self, tag, attrs):
        self.handle_prev()
        # <p> and <br> cause a newline to be output
        if tag == 'p' or tag == 'br':
            self.newline_active = True
        self.prepare_next(False)

    def handle_data(self, data):
        self.handle_prev()
        # Mark as text to be kept if not inside <style> or <script>
        if not self.style_active and not self.script_active:
            self.data_active = True
        self.prepare_next(self.data_active)

    def handle_comment(self, data):
        self.handle_prev()
        self.prepare_next(False)
        return

    def handle_entityref(self, name):
        try:
            ch = chr(name2codepoint[name])
        except:
            # invalid reference is probably broken HTML, e.g. A&E
            ch = 0
        self.handle_prev()
        # Prepare for next item
        self.ref_char = ch
        self.prepare_next(False)
        return

    def handle_charref(self, name):
        try:
            if name.startswith('x'):
                ch = chr(int(name[1:], 16))
            else:
                ch = chr(int(name))
        except:
            # Probably broken HTML
            ch = 0
        self.handle_prev()
        # Prepare for next item
        self.ref_char = ch
        self.prepare_next(False)
        return

    def handle_decl(self, data):
        self.handle_prev()
        self.prepare_next(False)
        return

def test_RedactingHTMLParser():
    parser = RedactingHTMLParser()
    html_str = """<!DOCTYPE>
<html>
<p/>

<!--<a href="">
	comment
-->
<style>
{
	 stuff
		 # comment
}
</style>
<p style="things; more">Hello&nbsp;World&lt;&gt;
<BR>new line
</p>
</html>"""
    expected="""          
      

  


                            
                                         

                      
Hello      World<   >   

  
new line
    
       """
    parser.feed(html_str)
    result = parser.result()
    assert(len(html_str) == len(result))
    assert(result == expected)


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

def redact_html_tags_in_string_simple(html_str, replace_char='.', replace_newline='\n'):
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

def test_redact_html_tags_in_string_simple():
    src =      '<script src="s.js"/> <SCRIPT lang="js"> script1\n </script> text1 <1 month\r\n<BR>text2 <script> script2 </script> text3&nbsp;</p>'
    # changing the \r to a space in the expected string also tests the string_match_ignore_linebreak function
    dest = redact_html_tags_in_string_simple(src, replace_char='.')
    expected = '.................... ..................................... text1 <1 month \n....text2 .......................... text3      ....'
    assert(string_match_ignore_linebreak(dest, expected))
    # Test replacing HTML with spaces
    dest = redact_html_tags_in_string_simple(src, replace_char=' ')
    expected = '                                                           text1 <1 month \n    text2                            text3          '
    assert(string_match_ignore_linebreak(dest, expected))
    # Test the newline replacement
    dest = redact_html_tags_in_string_simple(src, replace_char=' ', replace_newline=' ')
    expected = '                                                           text1 <1 month      text2                            text3          '
    assert(string_match_ignore_linebreak(dest, expected))
    # Test squashing HTML and newlines
    dest = redact_html_tags_in_string_simple(src, replace_char='', replace_newline='')
    expected = '  text1 <1 monthtext2  text3      '
    assert(string_match_ignore_linebreak(dest, expected))


# ---------------------------------------------------------------------

def redact_html_tags_in_string(html_str, replace_char=' ', replace_newline='\n'):
    """ Remove all HTML tags from the string and return the new string.
    Does not try to preserve the original string length."""
    parser = RedactingHTMLParser(replace_char = replace_char)
    parser.feed(html_str)
    text_str = parser.result()
    return(text_str)

def test_redact_html_tags_in_string():
    dest = redact_html_tags_in_string('<!DOCTYPE fake> <style>stuff </style>hello <p>world')
    expected = '                                     hello \n \nworld'
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
