#!/usr/bin/env bash
set -e
set -o pipefail
set -x

# Install dependencies
#mozroots --import --sync --quiet

# Build scriptcs
if false; then
  #mono ./.nuget/NuGet.exe restore ./iCSharp.sln

  cd ./Engine

  # If brew is given as parameter, use the brew build-script
  if [ "$1" == "brew" ]
  then
    ./build_brew.sh
  else
    ./build.sh
  fi

  cd ../

fi

# Build iCSharp
mkdir -p build/Release/bin
xbuild ./iCSharp.sln /property:Configuration=Release /nologo /verbosity:normal

# Copy files safely
for line in $(find ./*/bin/Release/*); do 
	 cp $line ./build/Release/bin
done
