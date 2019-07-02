using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Experimental.Graph;
using Newtonsoft.Json;

namespace SlnfGen
{
    public class SlnfGen
    {
        private static string _slnDirectory;

        public string Create(string dirsProj)
        {
            var slnFullPath = GetSlnAbove(Directory.GetCurrentDirectory());
            var relativeSlnPath = MakeRelative(Environment.CurrentDirectory, slnFullPath);
            _slnDirectory = Path.GetDirectoryName(slnFullPath);

            // Load the graph and create the project.
            // Note: We want to filter out traversal nodes (dirs.proj) and make all the paths relative
            var graph = new ProjectGraph(dirsProj);
            var sln = new
            {
                solution = new
                {
                    path = relativeSlnPath,
                    projects = graph.ProjectNodes.Where(NotTraversalNode).Select(RelativePathString).Distinct()
                }
            };


            // Make sure everything exists
            if (!File.Exists(sln.solution.path))
            {
                Console.WriteLine($"Sln file doesn't exist: {sln.solution.path}");
            }
            else
            {
                Console.WriteLine($"Using '{sln.solution.path}':");
            }

            var originalDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _slnDirectory;

            foreach (var file in sln.solution.projects)
            {
                if (!File.Exists(file))
                {
                    Console.WriteLine($"Project file doesn't exist: {file}");
                }
                else
                {
                    Console.WriteLine($"  Discovered {file}");
                }
            }

            Environment.CurrentDirectory = originalDir;

            var json = JsonConvert.SerializeObject(sln, Formatting.Indented);
            return json;
        }

        private static bool NotTraversalNode(ProjectGraphNode node)
        {
            // Really anything that ends in .proj probably shouldn't be loaded in VS (file copy projects, etc.)
            return !node.ProjectInstance.FullPath.Contains(".proj");
        }

        private static string RelativePathString(ProjectGraphNode node)
        {
            // Projects listed in the slnf need to be relative to the sln not the slnf file even
            // though the sln file is relative to the slnf location.
            return MakeRelative(_slnDirectory, node.ProjectInstance.FullPath);
        }

        private static string GetSlnAbove(string currentDirectory)
        {
            var current = new DirectoryInfo(currentDirectory);
            while (true)
            {
                var slns = current.GetFiles("*.sln");
                if (slns.Length == 1)
                {
                    return slns[0].FullName;
                }

                if (slns.Length > 1)
                {
                    var sln = slns.ToList().OrderBy(s => s.Name.Length).First();

                    Console.WriteLine($"More than one sln found in {current.FullName}! Using {sln.Name}");
                    return sln.FullName;
                }

                current = current.Parent ?? throw new Exception($"Couldn't find a .sln above {currentDirectory}");
            }
        }

        /// <summary>
        /// Given the absolute location of a file, and a disc location, returns relative file path to that disk location. 
        /// Throws UriFormatException.
        /// </summary>
        /// <param name="basePath">
        /// The base path we want to relativize to. Must be absolute.  
        /// Should <i>not</i> include a filename as the last segment will be interpreted as a directory.
        /// </param>
        /// <param name="path">
        /// The path we need to make relative to basePath.  The path can be either absolute path or a relative path in which case it is relative to the base path.
        /// If the path cannot be made relative to the base path (for example, it is on another drive), it is returned verbatim.
        /// If the basePath is an empty string, returns the path.
        /// </param>
        /// <returns>relative path (can be the full path)</returns>
        internal static string MakeRelative(string basePath, string path)
        {
            //ErrorUtilities.VerifyThrowArgumentNull(basePath, "basePath");
            //ErrorUtilities.VerifyThrowArgumentLength(path, "path");

            if (basePath.Length == 0)
            {
                return path;
            }

            Uri baseUri = new Uri(EnsureTrailingSlash(basePath), UriKind.Absolute); // May throw UriFormatException

            Uri pathUri = CreateUriFromPath(path);

            if (!pathUri.IsAbsoluteUri)
            {
                // the path is already a relative url, we will just normalize it...
                pathUri = new Uri(baseUri, pathUri);
            }

            Uri relativeUri = baseUri.MakeRelativeUri(pathUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.IsAbsoluteUri ? relativeUri.LocalPath : relativeUri.ToString());

            string result = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return result;
        }

        /// <summary>
        /// If the given path doesn't have a trailing slash then add one.
        /// </summary>
        /// <param name="fileSpec">The path to check.</param>
        /// <returns>A path with a slash.</returns>
        internal static string EnsureTrailingSlash(string fileSpec)
        {
            if (!EndsWithSlash(fileSpec))
            {
                fileSpec += Path.DirectorySeparatorChar;
            }

            return fileSpec;
        }

        /// <summary>
        /// Ensures the path does not have a trailing slash.
        /// </summary>
        internal static string EnsureNoTrailingSlash(string path)
        {
            if (EndsWithSlash(path))
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }

        /// <summary>
        /// Indicates if the given file-spec ends with a slash.
        /// </summary>
        /// <owner>SumedhK</owner>
        /// <param name="fileSpec">The file spec.</param>
        /// <returns>true, if file-spec has trailing slash</returns>
        internal static bool EndsWithSlash(string fileSpec)
        {
            return (fileSpec.Length > 0)
                ? IsSlash(fileSpec[fileSpec.Length - 1])
                : false;
        }
        /// <summary>
        /// Indicates if the given character is a slash. 
        /// </summary>
        /// <owner>SumedhK</owner>
        /// <param name="c"></param>
        /// <returns>true, if slash</returns>
        internal static bool IsSlash(char c)
        {
            return ((c == Path.DirectorySeparatorChar) || (c == Path.AltDirectorySeparatorChar));
        }

        /// <summary>
        /// Helper function to create an Uri object from path.
        /// </summary>
        /// <param name="path">path string</param>
        /// <returns>uri object</returns>
        private static Uri CreateUriFromPath(string path)
        {
            //ErrorUtilities.VerifyThrowArgumentLength(path, "path");

            Uri pathUri = null;

            // Try absolute first, then fall back on relative, otherwise it
            // makes some absolute UNC paths like (\\foo\bar) relative ...
            if (!Uri.TryCreate(path, UriKind.Absolute, out pathUri))
            {
                pathUri = new Uri(path, UriKind.Relative);
            }

            return pathUri;
        }
    }
}
