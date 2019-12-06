cd /D "%~dp0"
SET output=%~dp0target\distrib\publish\
SET slnFile=%~dp0\AppServiceSample.sln

rem Build and publish the solution
dotnet build --no-incremental --no-restore "%slnFile%" /p:DeployOnBuild=true /p:WebPublishMethod=Package /p:PublishProfile=Release /p:PackageAsSingleFile=true --output "%output%"
if %errorlevel% neq 0 (
    popd
    echo "Failed to build and publish the solution correctly. Error level is %ERRORLEVEL%"
    exit /B %errorlevel%
)

rem Update the version for buildver.txt used by EV2
Copy "%~dp0/buildver.txt" "%output%/buildver.txt"
powershell -File "%~dp0UpdateVersion.ps1" "%output%\buildver.txt"
if %ERRORLEVEL% neq 0 (
    popd
    echo "Failed to update version correctly. Error level is %ERRORLEVEL%"
	exit /b %ERRORLEVEL%
)

rem Exit with explicit 0 code so that build does not fail.
exit /B 0
