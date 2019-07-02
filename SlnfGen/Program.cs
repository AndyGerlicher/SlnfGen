using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using Microsoft.Build.Experimental.Graph;
using Microsoft.Build.Locator;
using Newtonsoft.Json;

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

                var dirsProj = args.Length == 1 ? args[0] : "dirs.proj";
                var slnfFile = Path.ChangeExtension(dirsProj, ".slnf");
                File.WriteAllText(slnfFile, new SlnfGen().Create(dirsProj));

                Process.Start(Path.Combine(instance.VisualStudioRootPath, "Common7", "IDE", "devenv.exe"), slnfFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Environment.Exit(-1);
            }
        }
    }
}
