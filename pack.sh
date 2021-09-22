#!bin/bash
mono /usr/lib/nuget/nuget.exe  pack Kernel/Kernel.csproj -MSBuildPath /usr/lib/mono/msbuild/15.0/bin -verbosity detailed -Properties Configuration=Release -IncludeReferencedProjects -Version 1.0.2

