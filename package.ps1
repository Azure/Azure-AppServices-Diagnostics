# Force all errors to stop the script and return with exit code 1
$ErrorActionPreference = 'Stop'

# Packages app
$OutBinFolder = "target\distrib"
Compress-Archive -Path "$OutBinFolder\publish\*" -DestinationPath "$OutBinFolder\AppServiceSample.zip" -Force
