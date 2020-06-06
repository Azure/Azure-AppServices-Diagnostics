@echo off
pushd "%~dp0"

:: Build all the projects in the solution
dotnet build --no-incremental --no-restore -c Release -v detailed %~dp0Diagnostics.sln"

IF %ERRORLEVEL% NEQ 0 (
echo "Build Failed."
exit /b %errorlevel%
)

popd
