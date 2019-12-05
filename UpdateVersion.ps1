param (
    [Parameter(Mandatory=$true)][string]$filePath
)
# Force all errors to stop the script and return with exit code 1
$ErrorActionPreference = 'Stop'

$version = If ($env:CDP_FILE_VERSION_NUMERIC) { $env:CDP_FILE_VERSION_NUMERIC } Else { "1.0.0.91" }

Write-Host "Update version numbers to $version"
Write-Host ("Updating version in file " + $filePath)
(Get-Content $filePath) -replace "<version>", $version | out-file $filePath -Force -Encoding ASCII
