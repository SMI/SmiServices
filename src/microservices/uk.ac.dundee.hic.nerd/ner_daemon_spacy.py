#!/usr/bin/env python3
# 1.04 arb Mon  1 Mar 17:26:46 GMT 2021 - increase timeout, use keepalives.
# 1.03 arb Sat 20 Feb 13:46:52 GMT 2021 - timeout after 60 seconds tries to
#      return nulnul just in case IsIdentifiable is listening.
#      Increase buffer size in case HTML or a PDF is pasted in.
# 1.02 arb Thu 18 Feb 14:22:41 GMT 2021 - additional entity types for scispacy,
#      changed default model from sm to md, added command line options,
#      allow multiple models to be specified, use scispacy if installed.
#      Change all entity names to UPPERCASE to match Stanford CoreNLP.
# 1.01 arb Tue 11 Feb 13:40:40 GMT 2020 - lock call to nlp in case not thread-safe
# Runs a NER daemon like NERd but using SpaCy for NER not Stanford.
# PROTOCOL:
#  Reads a nul-terminated string, responds with nul-separated and nul-terminated
#  string having fields: classification, start char, named entity
#  and a final nul when no more entities have been detected.
# INSTALL:
#  pip install spacy and pip install the model package first.
# USAGE:
#  -y default.yaml (for LogsRoot)
#  -m spacy model
#  -p port
# NOTE:
#  Unlike the java version it returns phrases not just words.
#  If scispacy is installed you can specify a scispacy model.
# TODO:
#  Add options to yaml for specifying port, model, etc.
# TEST:
#  printf "hello John\0\0" | netcat -W 1 localhost 1881

default_spacy_model = 'en_core_web_md'  # the language model for NER, en_core_web_md
default_port = 1882                     # 1882 is not NERd, use 1881 to replace NERd
prog_name   = 'NERd_spacy'              # for logging and error messages
log_root_env = 'SMI_LOGS_ROOT'          # name of environment variable if LogsRoot not in yaml
text_encoding = 'utf-8'                 # XXX assumed

import argparse
import logging, logging.handlers
import os
import socket
import sys
# Describe how to use virtualenv if SpaCy not found
try:
    import spacy
except:
    raise Exception("Cannot load SpaCy module, try source $SMI_ROOT/lib/python3/virtualenvs/spacy2/$(hostname -s)/bin/activate")
# Only issue a warning if SciSpaCy not found
try:
    import scispacy # not yet. Use model en_core_sci_md, or en_ner_bc5cdr_md
except:
    print('Warning: SciSpaCy not installed (do not try to use a scispacy language model)', file=sys.stderr)
import threading
import yaml


# ---------------------------------------------------------------------
# Map from spaCy's output to a member of FailureClassification enum
# so that it's compatible with the Stanford CoreNLP output in IsIdentifiable.
# Enum.Parse ignores case so Person and PERSON are ok, but must be one of:
# PrivateIdentifier, Location, Person, Organization, Money, Percent, Date, Time, PixelText, Postcode.
spacy_entity_to_FailureClassification_map = {
    'PERSON': 'PERSON',    # People, including fictional.
    'NORP': '',            # Nationalities or religious or political groups.
    'FAC': '',             # Buildings, airports, highways, bridges, etc.
    'ORG': 'ORGANIZATION', # Companies, agencies, institutions, etc.
    'GPE': 'LOCATION',     # Countries, cities, states.
    'LOC': '',             # Non-GPE locations, mountain ranges, bodies of water.
    'PRODUCT': '',         # Objects, vehicles, foods, etc. (Not services.)
    'EVENT': '',           # Named hurricanes, battles, wars, sports events, etc.
    'WORK_OF_ART': '',     # Titles of books, songs, etc.
    'LAW':      '',        # Named documents made into laws.
    'LANGUAGE': '',        # Any named language.
    'DATE':    'DATE',     # Absolute or relative dates or periods.
    'TIME':    'TIME',     # Times smaller than a day.
    'PERCENT': 'PERCENT',  # Percentage, including ”%“.
    'MONEY':   'MONEY',    # Monetary values, including unit.
    'QUANTITY': '',        # Measurements, as of weight or distance.
    'ORDINAL': '',         # "first", "second", etc.
    'CARDINAL': '',        # Numerals that do not fall under another type.
    # When trained on Wikipedia you get different entity meanings
    'PER': 'PERSON',       # Named person or family.
   #'LOC': 'LOCATION' ,    # Name of politically or geographically defined location (cities, provinces, countries, international regions, bodies of water, mountains). This clashes with the above so commented out.
    'ORG': 'ORGANIZATION', # Named corporate, governmental, or other organizational entity.
    'MISC': '',            # Miscellaneous entities, e.g. events, nationalities, products or works of art.
    # SciSpaCy has additional entity types depending on which corpus was used for training.
    # See a description here: https://github.com/allenai/scispacy/issues/79
    # en_ner_craft_md =
    'GGP': '',           # Genes
    'SO': '',            # Sequence ontology
    'TAXON': '',         # NCBI taxonomy
    'CHEBI': '',         # Chemical entities of biological interest
    'GO': '',            # Gene ontology
    'CL': '',            # Cell lines
    # en_ner_jnlpba_md =
    'DNA': '',           #
    'CELL_TYPE': '',     #
    'CELL_LINE': '',     #
    'RNA': '',           #
    'PROTEIN': '',       #
    # en_ner_bc5cdr_md =
    'DISEASE': '',       #
    'CHEMICAL': '',      #
    # en_ner_bionlp13cg_md =
    'AMINO_ACID': '',                      #
    'ANATOMICAL_SYSTEM': '',               #
    'CANCER': '',                          #
    'CELL': '',                            #
    'CELLULAR_COMPONENT': '',              #
    'DEVELOPING_ANATOMICAL_STRUCTURE': '', #
    'GENE_OR_GENE_PRODUCT': '',            #
    'IMMATERIAL_ANATOMICAL_ENTITY': '',    #
    'MULTI-TISSUE_STRUCTURE': '',          #
    'ORGAN': '',                           #
    'ORGANISM': '',                        #
    'ORGANISM_SUBDIVISION': '',            #
    'ORGANISM_SUBSTANCE': '',              #
    'PATHOLOGICAL_FORMATION': '',          #
    'SIMPLE_CHEMICAL': '',                 #
    'TISSUE': '',                          #
}


# ---------------------------------------------------------------------
def setup_logging(log_dir = None):
    """ Create rotating log files in NERd_spacy subdirectory of the given log dir.
    If no dir is specified then use SMI_LOGS_ROOT or current directory.
    """
    if log_dir is None:
        log_dir = os.environ.get(log_root_env, '.') # SMI_LOGS_ROOT, or current dir if unset
    log_dir = os.path.join(log_dir, prog_name)
    os.makedirs(log_dir, exist_ok = True)
    log_file = os.path.join(log_dir, f'{prog_name}.log')
    log_fd = logging.handlers.RotatingFileHandler(filename=log_file, maxBytes=64*1024*1024, backupCount=9)
    log_handlers = [ log_fd ] # ,logging.StreamHandler(sys.stderr)
    logging.basicConfig(level=logging.DEBUG, handlers=log_handlers,
        format='[%(asctime)s] {%(filename)s:%(lineno)d} %(levelname)s - %(message)s')


# ---------------------------------------------------------------------
class ThreadedServer(object):
    """ A multi-threaded NER daemon, can be initialised with a list of
    SpaCy models otherwise uses the default single model. Listens on the
    given port for a nul-terminated string to parse.
    """
    def __init__(self, host, port, spacy_model_list = None):
        self.host = host
        self.port = port
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_KEEPALIVE, 1)
        self.sock.bind((self.host, self.port))
        self.nlp_list = []
        for model in spacy_model_list:
            self.nlp_list.append( spacy.load(model, disable=["tagger", "parser"]) )
        self.lock = threading.Lock()
        logging.debug(f'Initialised, with model {spacy_model_list}')

    def run(self):
        self.sock.listen(5)
        while True:
            client, address = self.sock.accept()
            client.settimeout(6*60*60) # six hours
            threading.Thread(target = self.listenToClient, args = (client, address)).start()

    def listenToClient(self, client, address):
        size = 128*1024
        self.lock.acquire()
        while True:
            try:
                data = client.recv(size)
                if data:
                    response = ''
                    # Replace nul with dot, decode from utf-8 bytes to string, do NER
                    # either doc=self.nlp OR
                    # self.nlp.pipe takes a generator and returns a generator (and runs threaded)
                    def text_generator(data):
                        yield data.replace(b'\0', b'.').decode(text_encoding, errors='strict')
                    for nlp in self.nlp_list:
                        for ner_doc in nlp.pipe(text_generator(data), disable=['tagger','parser']):
                            for entity in ner_doc.ents:
                                label = spacy_entity_to_FailureClassification_map.get(entity.label_, '')
                                if label == '': continue
                                response += ("%s\0%d\0%s\0" % (label, entity.start_char, entity.text))
                    if response == '': response += '\0'
                    response += '\0'
                    # Convert from utf-8 back to bytes for transmission
                    client.send(response.encode(text_encoding, errors='strict'))
                    logging.debug(repr(data) + ' -> ' + repr(response))
                else:
                    self.lock.release()
                    return
            except Exception as e:
                logging.exception("NERd exception "+repr(e))
                self.lock.release()
                client.send(b'\0\0')
                client.close()
                return False
        self.lock.release()
        return


# ---------------------------------------------------------------------
if __name__ == "__main__":
    """ Main program
    """
    parser = argparse.ArgumentParser(description = prog_name)
    parser.add_argument('-y', '--yaml', dest='yaml', action="store", help='the default.yaml config file')
    parser.add_argument('-m', '--model', dest='model', action="append", help='a SpaCy language model')
    parser.add_argument('-p', '--port', dest='port', action="store", help='port number, default 1881')
    args = parser.parse_args()

    host_name=''
    port_num = int(args.port) if args.port else default_port

    spacy_models = args.model if args.model else [default_spacy_model]

    if args.yaml:
        with open(args.yaml) as fd:
            yaml_dict = yaml.safe_load(fd)
    else:
        yaml_dict = {}

    logs_root = yaml_dict.get('LoggingOptions', {}).get('LogsRoot', None)
    print(f'Logging to {logs_root}')

    setup_logging(log_dir = logs_root)
    logging.debug(f'using yaml {args.yaml}')
    logging.debug(f'using port {host_name}:{port_num}')
    logging.debug(f'using model {spacy_models}')

    ThreadedServer(host_name, port_num, spacy_model_list = spacy_models).run()
