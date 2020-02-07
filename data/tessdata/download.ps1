if (test-path "eng.traineddata"){

	return 0
}
ELSE
{
	
	wget "https://github.com/tesseract-ocr/tessdata/raw/master/eng.traineddata" -outfile "./eng.traineddata"
}


