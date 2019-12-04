cd /D "%~dp0"
dotnet test AppServiceSample.sln --logger:trx
exit /B %ERRORLEVEL%
