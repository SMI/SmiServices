# Name Entity Recognition Daemon

Primary Author: [James A Sutherland](https://github.com/jas88)
Python author: Andrew Brooks

## Contents

1. [Background](#background)
1. [Setup](#setup)
1. [Running](#running)


## Background

This standalone process is designed to classify text strings sent by the [IsIdentifiable](../Microservices.IsIdentifiable/README.md#socket-rules) application. It accepts TCP connections on localhost port 1881, returning classification results as expected by the IsIdentifiable microservice.

There are two implementations, one in Java which uses the [Stanford CoreNLP](https://stanfordnlp.github.io/CoreNLP/) library and one in Python which uses the [SpaCy](https://spacy.io/) library.

The [Stanford CoreNLP NER algorithm](https://stanfordnlp.github.io/CoreNLP/ner.html#description) recognizes named (PERSON, LOCATION, ORGANIZATION, MISC), numerical (MONEY, NUMBER, ORDINAL, PERCENT), and temporal (DATE, TIME, DURATION, SET) entities (12 classes). Adding the regexner annotator and using the supplied RegexNER pattern files adds support for the fine-grained and additional entity classes but the additional annotator is not currently used here. 

The Python version recognises a different set of entities, and with different language models can recognise drugs, diseases, cells, etc. although these are not required for named entities. It tries to map its entity type names to match those returned by CoreNLP so that both can be used in a ConsensusRule in IsIdentifiable.

## Setup

No setup is required for the Java version, just run the jar file as documented below. The "<&- &" will cause it to disconnect from the terminal once initialised and run as a daemon. (For development use, you can also skip that and terminate it with ctrl-C on the console when finished.)

The Python program now requires a YAML configuration file to determine the log file location. It also requires that the SpaCy and optionally the SciSpaCy packages have been installed, if not globally then into a virtual environment. The same environment must also have the required SpaCy language model installed. Note that SpaCy version 2 (eg. 2.2.1) and SciSpacy version 0.2.4 must be used (as of Feb 2021) because SpaCy v3 uses a new architecture and SciSpaCy has not caught up yet (at least, not for NER).

## Running

`java -jar nerd.jar <&- &`

To shutdown again:

`fuser -k -TERM -n tcp 1881`

The Python version is:

`ner_daemon_spacy.py --yaml default.yaml --model en_core_web_md --port 1882`

The yaml must have `LoggingOptions | LogsRoot`.

The port should be different from NERd if both are to run in parallel.

Models are: `en_core_web_md` (the default), `en_core_sci_md` (scispacy, but warning: does not return entity types so no use for finding PII), `en_ner_bionlp13cg_md` and `en_ner_bc5cdr_md` (scispacy models for drugs and diseases). Additional SpaCy models can be used which are much larger, eg. `en_core_web_lg`.
