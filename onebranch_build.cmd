@echo off
pushd "%~dp0"

:: Build all the projects in the solution
::dotnet build --no-incremental --no-restore -c Release -v detailed %~dp0Diagnostics.sln"

echo "Check if Node is installed"
npm help
npm --version

dotnet build --no-incremental --no-restore -c Release -v minimal %~dp0src\Diagnostics.RuntimeHost\Diagnostics.RuntimeHost.csproj
dotnet build --no-incremental --no-restore -c Release -v minimal %~dp0src\Diagnostics.CompilerHost\Diagnostics.CompilerHost.csproj

IF %ERRORLEVEL% NEQ 0 (
echo "Build Failed."
exit /b %errorlevel%
)

:: Publish Compiler Host to Build Location
echo\
echo "------------------- Publishing Compiler Host to build directory -------------------"
echo\
dotnet publish "%~dp0src\Diagnostics.CompilerHost\Diagnostics.CompilerHost.csproj" -c Release -o "%~dp0build\antares.external.diagnostics.compilerhost.1.0.0" --no-build

IF %ERRORLEVEL% NEQ 0 (
echo "Diagnostics.CompilerHost Publish Failed."
exit /b %errorlevel%
)
echo\

:: Publish Runtime Host to Build Location
echo\
echo "------------------- Publishing Runtime Host to build directory -------------------"
echo\
dotnet publish "%~dp0src\Diagnostics.RuntimeHost\Diagnostics.RuntimeHost.csproj" -c Release -o "%~dp0build\antares.external.diagnostics.runtimehost.1.0.0" --no-build

IF %ERRORLEVEL% NEQ 0 (
echo "Diagnostics.RuntimeHost Publish Failed."
exit /b %errorlevel%
)

popd
