pushd "%~dp0"
dotnet restore AppServiceSample.sln || exit /b 1
