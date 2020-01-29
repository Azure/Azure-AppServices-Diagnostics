pushd "%~dp0"

dotnet test AppServiceSample.sln /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura --logger:trx

if %ERRORLEVEL% neq 0 (
    popd
    exit /B %ERRORLEVEL%
)

coveragetool\reportgenerator -reports:**\coverage.cobertura.xml -targetdir:.\reports "-reportTypes:htmlInline;Cobertura

popd
exit /B %ERRORLEVEL%
