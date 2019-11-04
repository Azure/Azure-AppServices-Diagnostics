@echo off

:: Delete existing build drop
@RD /S /Q "build"

:: Build all the projects in the solution
dotnet build Diagnostics.sln

IF %ERRORLEVEL% NEQ 0 (
echo "Build Failed."
exit /b %errorlevel%
)
echo\

:: Publish Compiler Host to Build Location
echo\
echo "------------------- Publishing Compiler Host to build directory -------------------"
echo\
dotnet publish src\\Diagnostics.CompilerHost\\Diagnostics.CompilerHost.csproj -c Release -o build\\antares.external.diagnostics.compilerhost.1.0.0

IF %ERRORLEVEL% NEQ 0 (
echo "Diagnostics.CompilerHost Publish Failed."
exit /b %errorlevel%
)
echo\

:: Publish Runtime Host to Build Location
echo\
echo "------------------- Publishing Runtime Host to build directory -------------------"
echo\
dotnet publish src\\Diagnostics.RuntimeHost\\Diagnostics.RuntimeHost.csproj -c Release -o build\\antares.external.diagnostics.runtimehost.1.0.0

echo\
echo "------------------- Publishing AI Projects to build directory --------------------"
echo\
echo D | xcopy /E /Y /exclude:src\\Diagnostics.AIProjects\\excludeFromPublish.txt src\\Diagnostics.AIProjects build\\antares.external.diagnostics.aiprojects.1.0.0

IF %ERRORLEVEL% NEQ 0 (
echo "Diagnostics.RuntimeHost Publish Failed."
exit /b %errorlevel%
)
