using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Locator;

namespace SlnfGen
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var instance = MSBuildLocator.QueryVisualStudioInstances().First();
                MSBuildLocator.RegisterInstance(instance);

                var dirsProj = args.Length > 0 ? args[0] : "dirs.proj";
                var slnfFile = Path.ChangeExtension(dirsProj, ".slnf");
                File.WriteAllText(slnfFile, new SlnfGen().Create(dirsProj));

                if (args.Length > 1 && args[1] == "novs")
                {
                    Console.WriteLine($"Wrote {slnfFile}");
                }
                else
                {
                    var vsPath = Path.Combine(instance.VisualStudioRootPath, "Common7", "IDE", "devenv.exe");
                    Console.WriteLine($"{vsPath} {slnfFile}");
                    Process.Start(vsPath, slnfFile);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Environment.Exit(-1);
            }
        }
    }
}
