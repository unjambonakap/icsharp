#!/bin/bash
set -eux
ver=$1
mono /usr/lib/nuget/nuget.exe  pack Kernel/Kernel.csproj -MSBuildPath /usr/lib/mono/msbuild/15.0/bin -verbosity detailed -Properties Configuration=Debug -IncludeReferencedProjects -Version $ver

last_lnk=iCSharp.Kernel.last.nupkg
rm $last_lnk || true
ln -sv $PWD/iCSharp.Kernel.$ver.nupkg $last_lnk

