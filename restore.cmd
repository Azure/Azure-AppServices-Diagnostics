pushd "%~dp0"
dotnet restore "%~dp0..\AppServiceSample.sln" || exit /b 1
