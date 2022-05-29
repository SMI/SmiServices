# Compile a standalone Windows .exe

* Download miniconda and install it.
* Start Anaconda Prompt from the Start Menu. Install required software:

```
> cd Documents/src/miniconda3/envs
> conda create -n py_to_exe
> conda activate py_to_exe
> conda install nuitka
  ca-certificates    pkgs/main/win-64::ca-certificates-2022.2.1-haa95532_0
  certifi            pkgs/main/win-64::certifi-2021.10.8-py39haa95532_2
  nuitka             pkgs/main/noarch::nuitka-0.6.16-pyhd3eb1b0_1
  openssl            pkgs/main/win-64::openssl-1.1.1m-h2bbff1b_0
  pip                pkgs/main/win-64::pip-21.2.4-py39haa95532_0
  python             pkgs/main/win-64::python-3.9.7-h6244533_1
  setuptools         pkgs/main/win-64::setuptools-58.0.4-py39haa95532_0
  sqlite             pkgs/main/win-64::sqlite-3.37.2-h2bbff1b_0
  tzdata             pkgs/main/noarch::tzdata-2021e-hda174b7_0
  vc                 pkgs/main/win-64::vc-14.2-h21ff451_1
  vs2015_runtime     pkgs/main/win-64::vs2015_runtime-14.27.29016-h5e58377_2
  wheel              pkgs/main/noarch::wheel-0.37.1-pyhd3eb1b0_0
  wincertstore       pkgs/main/win-64::wincertstore-0.2-py39haa95532_2
> conda install git
> git clone https://github.com/SMI/SmiServices
> cd SmiServices/src/common/Smi_Common_Python
> python setup.py bdist_wheel
> pip install dist/SmiServices-5.0.1-py3-none-any.whl
Successfully installed PyYAML-6.0 SmiServices-5.0.1 deepmerge-1.0.1 mysql-connector-python-8.0.28 pika-1.2.0 protobuf-3.19.4 pydicom-2.2.2 pymongo-4.0.2
```

Now compile using nuitka

```
> cd ../../../applications/SRAnonTool
> python CTP_DicomToText.py
> python -m nuitka --follow-imports CTP_DicomToText.py
 it will download to 'AppData\Local\Nuitka\Nuitka\gcc\x86_64\10.2.0-11.0.0-8.0.0-r5'
 also ccache
> python -m nuitka --standalone --follow-imports CTP_DicomToText.py
 downloads 'depends/x86_64'
```

Some imports are hidden from nuitka so it missed them out and you get `ModuleNotFoundError`. Complex solution is to use hints but easier to identify each one and request it explicitly:

```
python -m nuitka --onefile --include-module=pydicom.encoders.gdcm --include-module=pydicom.encoders.pylibjpeg CTP_DicomToText.py
```

Run (you may need to fix up the yaml file though):
```
$env:SMI_ROOT="."
.\CTP_DicomToText.exe  -i ..\test\report10.dcm -o report10.txt -y ..\..\..\..\data\microserviceConfigs\default.yaml
```
