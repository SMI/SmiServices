#!/usr/bin/env python
# Read in report02.dcm and write report02mod.dcm
# with all instances of "The patient" replaced by one of the actual names
# found in the DICOM tags.

import random
import pydicom

ds = pydicom.dcmread('report02.dcm')

names = set()
for elem in ds.iterall():
    if elem.VR == 'PN':
        names.add(str(elem.value))

name_parts = set()
for name in names:
    # family name complex, given name complex, middle name, name prefix, name suffix
    for part in name.split('^'):
        for s in part.split(' '):
            if len(s) > 3:
                name_parts.add(s)
name_parts = list(name_parts)
print(name_parts)

print(random.sample(name_parts, 1)[0])

for elem in ds.iterall():
    if 'The patient' in str(elem.value):
        elem.value = str(elem.value).replace('The patient', random.sample(name_parts, 1)[0])
    if 'the patient' in str(elem.value):
        elem.value = str(elem.value).replace('the patient', random.sample(name_parts, 1)[0])

ds.save_as('report02mod.dcm')
