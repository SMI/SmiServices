#!/bin/bash
# CTP_SRAnonTool.sh
#   This script is called from CTP via the SRAnonTool config in default.yaml.
# It is called with -i DICOMfile -o OutputDICOMfile
# It extracts the text from DICOMfile, passes it through the SemEHR anonymiser,
# and reconstructs the structured text into OutputDICOMfile.
# NOTE: the -o file must already exist! because only the text part is updated.
# NOTE: semehr needs python2 but all other tools need python3.
# XXX TODO: copy the input to the output if it doesn't exist?

prog=$(basename "$0")
progdir=$(dirname "$0")
usage="usage: ${prog} [-d] [-v] [-e virtualenv] [-s semehr_root] [-y yaml] -i read_from.dcm  -o write_into.dcm"
options="dve:s:y:i:o:"
semehr_dir="/opt/semehr"
virtenv=""
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
log="$SMI_LOGS_ROOT/${prog}/${prog}.log"
mkdir -p `dirname "${log}"`
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
	  if [ -f "${input_doc}" ]; then rm -f "${input_doc}"; fi
	  if [ -f "${anon_doc}" ]; then rm -f "${anon_doc}"; fi
	  if [ -f "${anon_xml}" ]; then rm -f "${anon_xml}"; fi
	  # Prefer not to use rm -fr for safety
	  if [ -d "${semehr_input_dir}" ]; then rm -f "${semehr_input_dir}/"*; fi
	  if [ -d "${semehr_input_dir}" ]; then rmdir "${semehr_input_dir}"; fi
	  if [ -d "${semehr_output_dir}" ]; then rm -f "${semehr_output_dir}/"*; fi
	  if [ -d "${semehr_output_dir}" ]; then rmdir "${semehr_output_dir}"; fi
	fi
	# Tell user where log file is when failure occurs
	if [ $rc -ne 0 ]; then echo "See log file $log" >&2; fi
	exit $rc
}

# Default executable PATHs and Python libraries
export PATH=${PATH}:${SMI_ROOT}/bin:${SMI_ROOT}/scripts:${progdir}
if [ "$PYTHONPATH" == "" ]; then
	export PYTHONPATH=${SMI_ROOT}/lib/python3:${SMI_ROOT}/lib/python3/virtualenvs/semehr/$(hostname -s)/lib/python3.6/site-packages:${SMI_ROOT}/lib/python3/virtualenvs/semehr/$(hostname -s)/lib64/python3.6/site-packages
fi

# Command line arguments
while getopts ${options} var; do
case $var in
	d) debug=1;;
	v) verbose=1;;
	e) virtenv="$OPTARG";;
	y) default_yaml0="$OPTARG";;
	i) input_dcm="$OPTARG";;
	o) output_dcm="$OPTARG";;
	s) semehr_dir="$OPTARG";;
	?) echo "$usage" >&2; exit 1;;
esac
done
shift $(($OPTIND - 1))

if [ ! -f "$input_dcm" ]; then
	tidy_exit 2 "ERROR: cannot read input file '${input_dcm}'"
fi
if [ ! -f "$output_dcm" ]; then
	#tidy_exit 3 "ERROR: cannot write to ${output_dcm} because it must already exist"
	cp "$input_dcm" "$output_dcm"
fi

# Activate the virtual environment
if [ "$virtenv" != "" ]; then
	if [ -f "$virtenv/bin/activate" ]; then
		source "$virtenv/bin/activate"
	else
		echo "ERROR: Cannot activate virtual environment ${virtenv} - no bin/activate script" >&2
		exit 1
	fi
fi

# Find the config files, if not specified try SMI defaults otherwise in the repo
if [ "$default_yaml0" == "" ]; then
	if [ -f "$SMI_ROOT/configs/smi_dataExtract.yaml" ]; then
		default_yaml0="$SMI_ROOT/configs/smi_dataLoad_mysql.yaml"
		default_yaml1="$SMI_ROOT/configs/smi_dataExtract.yaml"
	else
		default_yaml0="${progdir}/../../../data/microserviceConfigs/default.yaml"
	fi
fi
if [ "$default_yaml1" == "" ]; then
	default_yaml1="$default_yaml0"
fi


# ---------------------------------------------------------------------
# Determine the SemEHR filenames - create per-process directories
semehr_input_dir=$(mktemp  -d -t input_docs.XXXX --tmpdir=${semehr_dir}/data)
semehr_output_dir=$(mktemp -d -t anonymised.XXXX --tmpdir=${semehr_dir}/data)
if [ "$semehr_input_dir" == "" ]; then
	tidy_exit 8 "Cannot create temporary directory in ${semehr_dir}/data"
fi
if [ "$semehr_output_dir" == "" ]; then
	tidy_exit 9 "Cannot create temporary directory in ${semehr_dir}/data"
fi

doc_filename=$(basename "$input_dcm")
input_doc="${semehr_input_dir}/${doc_filename}"
anon_doc="${semehr_output_dir}/${doc_filename}"
anon_xml="${semehr_output_dir}/${doc_filename}.knowtator.xml"

# ---------------------------------------------------------------------
# Convert DICOM to text
#  Reads  $input_dcm
#  Writes $input_doc
if [ $verbose -gt 0 ]; then
	echo "RUN: CTP_DicomToText.py  -y $default_yaml0 -y $default_yaml1 -i ${input_dcm} -o ${input_dcm}.SRtext"
fi
CTP_DicomToText.py  -y $default_yaml0 -y $default_yaml1 \
	-i "${input_dcm}" \
	-o "${input_doc}"  || tidy_exit 4 "Error $? from CTP_DicomToText.py while converting ${input_dcm} to ${input_doc}"

# ---------------------------------------------------------------------
# Run the SemEHR anonymiser using a set of private directories
#  Reads  $input_doc
#  Writes $anon_doc, and $anon_xml via the --xml option
#
semehr_anon.py -s "${semehr_dir}" -i "${input_doc}" -o "${anon_doc}" --xml || tidy_exit 5 "Error running SemEHR-anon given ${input_doc} from ${input_dcm}"
# If there's still no XML file then exit
if [ ! -f "$anon_xml" ]; then
	tidy_exit 6 "ERROR: SemEHR-anon failed to convert $input_doc to $anon_xml"
fi

# ---------------------------------------------------------------------
# Convert XML back to DICOM
#  Reads  $input_dcm and $anon_xml
#  Writes $output_dcm (must already exist)
if [ $verbose -gt 0 ]; then
	echo "RUN: CTP_XMLToDicom.py -y $default_yaml1 	-i $input_dcm -x $anon_xml -o $output_dcm"
fi
CTP_XMLToDicom.py -y $default_yaml1 \
	-i "$input_dcm" \
	-x "$anon_xml" \
	-o "$output_dcm"   || tidy_exit 7 "Error $? from CTP_XMLToDicom.py while redacting $output_dcm with $anon_xml"

tidy_exit 0 "Finished with ${input_dcm}"
