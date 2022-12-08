# Extraction

Describes the build-install-test procedure, not the deployment into production.

Main docs:
https://github.com/SMI/SmiServices/tree/master/src/microservices/com.smi.microservices.ctpanonymiser

https://github.com/SMI/SmiServices/tree/release/1.2.0#image-extraction-microservices

See also: the extraction-refactoring branch
https://github.com/SMI/SmiServices/tree/feature/extraction-refactoring/docs/extraction

Other docs:
https://uoe.sharepoint.com/sites/SMI/Shared%20Documents/Forms/AllItems.aspx
https://git.ecdf.ed.ac.uk/SMI/SmiServiceOps/blob/master/Planning/ExtractionFlags
https://github.com/HicServices/SMIPlugin/blob/master/Documentation/Images/ExtractionMicroservices.png

# Building

See elsewhere the documents for building the Java programs.

# Prerequisites for testing

A RabbitMQ instance is required - you can run a test version inside a Docker container:

```
sudo docker run -d --hostname my-rabbit --name some-rabbit-mgt -p 5671:5671 -p 5672:5672 -p 5673:5673 -p 15671:15671 -p 15672:15672 -p 25672:25672 rabbitmq:3-management
```

# ExtractImages

```console
# Example CSV file
cat > extractme.csv << _EOF
SeriesInstanceUID,foo
1.2.826.0.1.3680043.2.1125.1.78969117856457473538394301521877227,1
_EOF

Edit default.yaml (RabbitOptions and FileSystemOptions)
You could do this programmatically with
`yq_linux_amd64 write --inplace d FileSystemOptions.FileSystemRoot /tmp`
although the current version of yq loses comments and unnecessary quotes.

Login to rabbit (localhost:15672) and create exchanges:
TEST.RequestExchange
TEST.RequestInfoExchange
Add bindings from those exchanges to any queue (TEST.xxx)

ProjectNum=001
rmdir /tmp/${ProjectNum}/tmp  # program gives error if dir already exists

# Interactive - answer y to create messages, or run with --non-interactive
$ ./smi extract-images -y default.yaml -p "$ProjectNum" extractme.csv
```

Two messages are created:

```
{"KeyTag":"SeriesInstanceUID","ExtractionIdentifiers":["1.2.826.0.1.3680043.2.1125.1.78969117856457473538394301521877227"],"ExtractionJobIdentifier":"bb1cbed5-a666-4307-a781-5b83926eaa81","ProjectNumber":"001","ExtractionDirectory":"001/tmp","JobSubmittedAt":"2019-12-19T10:49Z"}
```

and

```
{"KeyTag":"SeriesInstanceUID","KeyValueCount":1,"ExtractionJobIdentifier":"bb1cbed5-a666-4307-a781-5b83926eaa81","ProjectNumber":"001","ExtractionDirectory":"001/tmp","JobSubmittedAt":"2019-12-19T10:49Z"}
```

# CohortExtractor

Requires MySQL instance? so not described (yet).

Creates messages containing fields:
DicomFilePath: Path to the original file
ExtractionDirectory: Extraction directory relative to the extract root
OutputPath: Output path for the anonymised file, relative to the extraction directory
See: ~/src/SmiServices/src/common/Smi.Common/Messages/Extraction/ExtractFileMessage.cs
Inherits ExtractMessage so:
Guid ExtractionJobIdentifier
string ProjectNumber
string ExtractionDirectory
DateTime JobSubmittedAt

# CTPanonymiser

`cd ~/src/SmiServices/src/microservices/com.smi.microservices.ctpanonymiser/target`

A whitelist is required, available from the old repo as:
https://raw.githubusercontent.com/HicServices/SMIPlugin/develop/Documentation/Anon/dicom-whitelist.script
https://raw.githubusercontent.com/HicServices/SMIPlugin/develop/Documentation/Anon/dicom-whitelist.script.new
(possibly identical content, apart from whitespace/newlines??)
or from the new repo in the directories:

```
SmiServices/src/applications/com.smi.applications.extractorcli/anonScript.txt
SmiServices/src/microservices/com.smi.microservices.ctpanonymiser/src/test/resources/dicom-anonymizer.script
```

Haven't yet determined which one is correct.

Edit `default.yaml` (RabbitOptions and FileSystemOptions)

Login to rabbit (http://localhost:15672/) and create exchanges:
TEST.ControlExchange
TEST.FatalLoggingExchange
TEST.FileStatusExchange
and queue: TEST.ExtractFileQueue
Check: do we need to add bindings from those exchanges to the queue?

Copy an input file into the directory relative to the root in default.yaml:

```
cp src/SmiServices/src/microservices/com.smi.microservices.ctpanonymiser/src/test/resources/image-000001.dcm /tmp
```

Create a fake message and send to TEST.ControlExchange:

```
python3 -m pip install pika
#!/usr/bin/env python3
msg_json = '{ "DicomFilePath": "image-000001.dcm", "ExtractionDirectory": "001/tmp/extractiondir/", "OutputPath": "output.dcm", "ExtractionJobIdentifier":"bb1cbed5-a666-4307-a781-5b83926eaa81",
"ProjectNumber":"001", "ExtractionDirectory":"001/tmp", "JobSubmittedAt":"2019-12-19T10:49Z" }'
hdr={'MessageGuid':'', 'OriginalPublishTimestamp':'', 'ProducerExacutableName':'test.py', 'ProducerProcessID': '0'}
import pika
connection = pika.BlockingConnection(pika.ConnectionParameters('localhost'))
channel = connection.channel()
# exchange='TEST.ControlExchange', '' to make binding straight to routing_key queue
channel.basic_publish(exchange='', routing_key='TEST.ExtractFileQueue', body=msg_json, properties=pika.BasicProperties(content_type='application/json', headers=hdr) )
```

Run:

```
java -jar CTPAnonymiser-portable-1.0.0.jar -a dicom-whitelist.script.new -y default.yaml
```

The output is written to `/tmp/001/tmp/output.dcm` in this example and the log file is in `logs/YYYY-MM-DD-hhmmss.log`

A 'success' message is published to TEST.FileStatusExchange containing:

```
{"DicomFilePath":"image-000001.dcm","AnonymisedFileName":"output.dcm","Status":0,"ExtractionJobIdentifier":"bb1cbed5-a666-4307-a781-5b83926eaa81","ProjectNumber":"001","ExtractionDirectory":"001/tmp","JobSubmittedAt":"2019-12-19T10:49Z"}
```

# IsIdentifiable

See the netcoreapp2.2 branch of IsIdentifiable here:
https://github.com/HicServices/IsIdentifiable/tree/netcoreapp2.2
with the changes required to build and run on dotnet core 2.2 Linux
(until such time as it's merged into master).
