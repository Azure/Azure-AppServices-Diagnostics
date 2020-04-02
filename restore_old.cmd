pushd "%~dp0"

dotnet restore "%~dp0..\AppServiceSample.sln"

if %ERRORLEVEL% neq 0 (
    popd
    exit /B %ERRORLEVEL%
)

rem Install code coverage tool
dotnet tool install dotnet-reportgenerator-globaltool --tool-path coveragetool

popd
exit /B %ERRORLEVEL%
