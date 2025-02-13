#! /bin/sh

# Set common flags for each release
FLAGS="-c Release /p:PublishSingleFile=true"
# All the runtimes to publish for
RUNTIMES=("linux-x64" "linux-arm64" "linux-arm" "win-x64")
# Exit on any error
set -e
# Echo every command
set -x

# Specific version for all runtimes
for RUNTIME in ${RUNTIMES[*]};
do
    dotnet publish $FLAGS -r $RUNTIME -o ./publish/$RUNTIME
done

# Generic cross-platform version
dotnet publish -c Release -o ./publish/cross-platform