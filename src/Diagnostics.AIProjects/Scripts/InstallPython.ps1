Add-Type -AssemblyName System.IO.Compression.FileSystem
copy "\\antaresdeployment\feeds\georegion\gr-feed-ant83-002\Installers.zip" "C:\Program Files\Installers.zip"

function Unzip
{
    param([string]$zipfile, [string]$outpath)

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

rm -r "C:\Program Files\python" -ea ig
Unzip "C:\Program Files\Installers.zip" "C:\Program Files\Installers"
Unzip "C:\Program Files\Installers\python.zip" "C:\Program Files\python"
rm "C:\Program Files\Installers.zip" -ea ig
rm -r "C:\Program Files\Installers" -ea ig

[Environment]::SetEnvironmentVariable(
    "Path",
    [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::User) + ";C:\Program Files\python\python",
    [EnvironmentVariableTarget]::User)

[Environment]::SetEnvironmentVariable(
    "Path",
    [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::User) + ";C:\Program Files\python\python\Scripts",
    [EnvironmentVariableTarget]::User)