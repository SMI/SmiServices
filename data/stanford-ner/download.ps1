$NER_VER="2016-10-31"

if (test-path "stanford-ner-$NER_VER"){

	return 0
}
ELSE
{
	
	wget "nlp.stanford.edu/software/stanford-ner-$NER_VER.zip" -outfile "./stanford-ner-$NER_VER.zip"
	
	Expand-Archive -Path stanford-ner-$NER_VER.zip -DestinationPath "."

	rm stanford-ner-$NER_VER.zip
}


