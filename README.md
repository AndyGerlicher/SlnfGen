# SlnfGen
Generates a .slnf file.

Example usage:
``` BASH
git clone https://github.com/AndyGerlicher/SlnfGen.git
cd SlnfGen
msbuild
cd SlnfGen
.\bin\Debug\net472\SlnfGen.exe SlnfGen.csproj
```

This will generate a solution filter file (`SlnfGen.slnf`) pointing to the first solution it finds walking up the tree (`/SlnfGen.sln`) which lists the `SlnfGen.csproj` project and all dependencies. This is not a great example because there's only 1 project, but it is useful for testing.
