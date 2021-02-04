#!/bin/bash
# CTP_SRAnonTool.sh
#   This script is called from CTP via the SRAnonTool config in default.yaml.
# It is called with -i DICOMfile -o OutputDICOMfile
# It extracts the text from DICOMfile, passes it through the SemEHR anonymiser,
# and reconstructs the structured text into OutputDICOMfile.
# NOTE: the -o file must already exist! because only the text part is updated.
# XXX TODO: copy the input to the output if it doesn't exist?

prog=$(basename "$0")
usage="usage: ${prog} [-d] [-v]  -i read_from.dcm  -o write_into.dcm"
options="dvi:o:"
log="$SMI_LOGS_ROOT/${prog}.log"
dcm=""
debug=0
verbose=0

# Default value if not set is for SMI
if [ "$SMI_ROOT" == "" ]; then
	echo "${prog}: WARNING: env var SMI_ROOT was not set, using default value" >&2
	export SMI_ROOT="/nfs/smi/home/smi"
fi
if [ "$SMI_LOGS_ROOT" == "" ]; then
	echo "${prog}: WARNING: env var SMI_LOGS_ROOT was not set, using default value" >&2
	export SMI_LOGS_ROOT="/beegfs-hdruk/smi/data/logs"
fi

# Configure logging
touch $log
echo "`date` $@" >> $log

# Error reporting and exit
tidy_exit()
{
	rc=$1
	msg="$2"
	echo "$msg" >&2
	echo "`date` $msg" >> $log
	# Tidy up, if not debugging
	if [ $debug -eq 0 ]; then
	  if [ -f "${input_dcm}.SRtext" ]; then rm -f "${input_dcm}.SRtext"; fi
	  if [ -f "${input_doc}" ]; then rm -f "${input_doc}"; fi
	  if [ -f "${anon_doc}" ]; then rm -f "${anon_doc}"; fi
	  if [ -f "${anon_xml}" ]; then rm -f "${anon_xml}"; fi
	fi
	exit $rc
}

# Default executable PATHs and Python libraries
export PATH=${PATH}:${SMI_ROOT}/bin:${SMI_ROOT}/scripts:$(dirname "$0")
export PYTHONPATH=${SMI_ROOT}/lib/python3

# Command line arguments
while getopts ${options} var; do
case $var in
	d) debug=1;;
	v) verbose=1;;
	i) input_dcm="$OPTARG";;
	o) output_dcm="$OPTARG";;
	?) echo "$usage" >&2; exit 1;;
esac
done
shift $(($OPTIND - 1))

if [ ! -f "$input_dcm" ]; then
	tidy_exit 2 "ERROR: cannot read ${input_dcm}"
fi
if [ ! -f "$output_dcm" ]; then
	tidy_exit 3 "ERROR: cannot write to ${output_dcm} because it must already exist"
fi

# Find the config files
if [ -d $SMI_ROOT/configs ]; then
	default_yaml0="$SMI_ROOT/configs/smi_dataLoad_mysql.yaml"
	default_yaml1="$SMI_ROOT/configs/smi_dataExtract.yaml"
else
	default_yaml0="$HOME/src/SmiServices/data/microserviceConfigs/default.yaml"
	default_yaml1="$default_yaml0"
fi

# Convert DICOM to text
if [ $verbose -gt 0 ]; then
	echo "RUN: CTP_DicomToText.py  -y $default_yaml0 -y $default_yaml1 -i ${input_dcm} -o ${input_dcm}.SRtext"
fi
CTP_DicomToText.py  -y $default_yaml0 -y $default_yaml1 \
	-i "${input_dcm}" \
	-o "${input_dcm}.SRtext"  || tidy_exit 4 "Error $? from CTP_DicomToText.py while converting ${input_dcm} to ${input_dcm}.SRtext"

# Run the SemEHR anonymiser
doc_filename=$(basename "$input_dcm")
input_doc="/data/input_docs/$doc_filename"
anon_doc="/data/anonymised/$doc_filename"
anon_xml="/data/anonymised/$doc_filename.knowtator.xml"
cp  "${input_dcm}.SRtext"  "$input_doc" || tidy_exit 5 "Cannot copy ${input_dcm}.SRtext to ${input_doc}"
if [ $verbose -gt 0 ]; then
	echo "RUN: /opt/semehr/CogStack-SemEHR/analysis/clinical_doc_wrapper.py"
fi
(cd /opt/semehr/CogStack-SemEHR/analysis; python2 ./clinical_doc_wrapper.py) >> $log 2>&1
rc=$?
if [ $rc -ne 0 ]; then
	tidy_exit $rc "Possible failure (exit code $rc) of SemEHR-anon given ${input_doc} from ${input_dcm}"
fi

if [ ! -f "$anon_xml" ]; then
	tidy_exit 6 "ERROR: SemEHR-anon failed to convert $input_doc to $anon_xml"
fi

# Convert XML back to DICOM
if [ $verbose -gt 0 ]; then
	echo "RUN: CTP_XMLToDicom.py -y $default_yaml1 	-i $input_dcm -x $anon_xml -o $output_dcm"
fi
CTP_XMLToDicom.py -y $default_yaml1 \
	-i "$input_dcm" \
	-x "$anon_xml" \
	-o "$output_dcm"   || tidy_exit 7 "Error $? from CTP_XMLToDicom.py while redacting $output_dcm with $anon_xml"

tidy_exit 0 "Finished with ${input_dcm}"
