[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://www.python.org/ftp/python/3.6.4/python-3.6.4-amd64.exe" -OutFile "c:/python-3.6.4-amd64.exe"
c:/python-3.6.4-amd64.exe /quiet InstallAllUsers=0 PrependPath=1 Include_test=0

py -m venv searchenv
.\searchenv\Scripts\activate
py -m pip install -r ..\SearchAPI\requirements.txt