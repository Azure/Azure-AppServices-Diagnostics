@echo off
pushd "%~dp0"

:: Build all the projects in the solution
::dotnet build --no-incremental --no-restore -c Release -v detailed %~dp0Diagnostics.sln"
dotnet build --no-incremental --no-restore -c Release -v detailed %~dp0src\Diagnostics.RuntimeHost\Diagnostics.RuntimeHost.csproj
dotnet build --no-incremental --no-restore -c Release -v detailed %~dp0src\Diagnostics.CompilerHost\Diagnostics.CompilerHost.csproj

IF %ERRORLEVEL% NEQ 0 (
echo "Build Failed."
exit /b %errorlevel%
)

popd
