cd /D "%~dp0"

rem Build the solution

dotnet build --no-restore AppServiceSample.sln /p:DeployOnBuild=true /p:WebPublishMethod=Package /p:PackageAsSingleFile=true

if %errorlevel% neq 0 (
    popd
    echo "Failed to build solution correctly. Error level is %ERRORLEVEL%"
    exit /B %errorlevel%
)

dotnet publish --no-restore -c Release -o "%~dp0target\distrib\publish" "%~dp0src\AspNetCoreSample\AspNetCoreSample.csproj"

if %errorlevel% neq 0 (
    popd
    echo "Failed to publish correctly. Error level is %ERRORLEVEL%"
    exit /B %errorlevel%
)

rem Package the app service.
powershell -File "%~dp0package.ps1"
if %errorlevel% neq 0 (
    popd
    echo "Failed to package correctly. Error level is %ERRORLEVEL%"
    exit /B %errorlevel%
)

rem Exit with explicit 0 code so that build does not fail.
exit /B 0
