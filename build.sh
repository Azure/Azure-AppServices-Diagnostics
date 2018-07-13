#!/bin/bash
# build Script

buildMode=$1
if [ -z "$1" ]; then
    buildMode="BuildOnly"
elif [[ "$buildMode" != "BuildOnly" ]] && [[ "$buildMode" != "BuildAndPublish" ]]; then
    printf '%s\n' "Unrecognized Input ($buildMode). Only running build..."
    buildMode="BuildOnly"
fi

SRC_DIR=src
TEST_DIR=tests
BUILD_DIR=build

# Deleting the build folder if already exists
if [ -d "$BUILD_DIR" ]; then
    printf '%s\n' "Removing Lock ($BUILD_DIR)"
    rm -rf "$BUILD_DIR"
fi

printf '\n\e[1;34m%-6s\e[m\n' "Starting Build"

# Build step
dotnet build Diagnostics.sln | tee /dev/stderr | grep 'Build succeeded.' &> /dev/null
buildRetVal=$?

if [ $buildRetVal -ne 0 ]; then
    exit $buildRetVal
fi

printf '\n\e[1;34m%-6s\e[m\n' "Running Tests"

# Test step
dotnet test $TEST_DIR | tee /dev/stderr | grep 'Test Run Successful.' &> /dev/null
testRetVal=$?

if [ $testRetVal -ne 0 ]; then
    exit $testRetVal
fi

printf '\n\e[1;34m%-6s\e[m\n' "Publishing Packages locally"

# Publish step
dotnet publish $SRC_DIR/Diagnostics.RuntimeHost/Diagnostics.RuntimeHost.csproj -c Release -o ../../$BUILD_DIR/Diagnostics.RuntimeHost
dotnet publish $SRC_DIR/Diagnostics.CompilerHost/Diagnostics.CompilerHost.csproj -c Release -o ../../$BUILD_DIR/Diagnostics.CompilerHost

printf '\n\e[1;34m%-6s\e[m\n' "Copying Nupsec File"

# Copy Nuspec File
src="$SRC_DIR/Diagnostics.nuspec"
dest="$BUILD_DIR/Diagnostics.nuspec"
cp -rf "$src" "$dest"
copyRetVal=$?

if [ $copyRetVal -ne 0 ] || [ "$buildMode" != "BuildAndPublish" ]; then
    exit $copyRetVal
fi

printf '\n\e[1;34m%-6s\e[m\n' "Generating Nuget Packages"

# Generating Nuget packages
mono nuget.exe pack $BUILD_DIR/Diagnostics.nuspec -OutputDirectory $BUILD_DIR
generateNugetPackageRetVal=$?

if [ $generateNugetPackageRetVal -ne 0 ]; then
    exit $generateNugetPackageRetVal
fi

printf '\n\e[1;34m%-6s\e[m\n' "Publishing Nuget Packages to nuget server"
# Publish Nuget Package to Source Server

CURRENT_PKG=$(find $BUILD_DIR -type f -name "*.nupkg")

if [ -z "$CURRENT_PKG" ]; then
    echo "Didnt find any nuget package in $BUILD_DIR"
    exit 1
fi

nugetPushOutput=$(mono nuget.exe push $CURRENT_PKG -Source https://api.nuget.org/v3/index.json $NUGET_PUSH_KEY)
nugetPushRetVal=$?
printf '%s\n' "$nugetPushOutput"

if [ $nugetPushRetVal -ne 0 ] && [[ $nugetPushOutput = *"Conflict"* ]]; then
    exit 0
fi

exit $nugetPushRetVal



