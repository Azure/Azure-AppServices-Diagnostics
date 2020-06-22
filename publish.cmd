@echo off

powershell Compress-Archive -Path "%~dp0build\antares.external.diagnostics.compilerhost.1.0.0\*" -DestinationPath "%~dp0build\DiagnosticsCompilerHost.zip"

powershell Compress-Archive -Path "%~dp0build\antares.external.diagnostics.runtimehost.1.0.0\*" -DestinationPath "%~dp0build\DiagnosticsRuntimeHost.zip"

exit /b 0

popd