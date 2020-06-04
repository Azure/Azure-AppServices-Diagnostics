pushd "%~dp0"

dotnet restore "%~dp0Diagnostics.sln"

if %ERRORLEVEL% neq 0 (
    popd
    exit /B %ERRORLEVEL%
)

rem Install code coverage tool
rem dotnet tool install dotnet-reportgenerator-globaltool --tool-path coveragetool

popd
exit /B %ERRORLEVEL%
