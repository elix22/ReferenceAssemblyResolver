using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using CommandLine;

namespace ReferenceAssemblyResolver
{

    public class Options
    {
        static  string[] assemblySearchPaths = new string[1] ;
       [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('o', "output", Default = "", Required = true, HelpText = "Set output folder to copy reference assmblies.")]
        public string OutputFolder { get; set; }

        [Option('a', "assembly", Default = "", Required = true, HelpText = "Set main assembly full path")]
        public string AssemblyPath { get; set; }

        [Option('s', "search", Default = "", Required = false, HelpText = "search paths for assemblies")]
        public string searchPaths { get; set; }

    }

    /*
     * ReferenceAssemblyResolver.exe   --output "E:/Development/ReferenceAssemblyResolver/temp2" --assembly "E:\Development\TestProject\bin\Release\netcoreapp3.1\Game.dll" --search "E:\Development\TestProject\bin\Release\netcoreapp3.1,E:\Development\TestProject\libs\dotnet\bcl\android,E:\Development\TestProject\libs\dotnet\bcl\android\Facades"
     */
    class Program
    {

        static bool verbose = false;

        static string assemblyPath = "";

        static string destAssemblyPath = "";

        static string[] assemblySearchPaths = {""};

        static List<string> assembliesList = new List<string>();

        static bool referenceExist(AssemblyName reference)
        {
            bool res = false;

            foreach (var path in assemblySearchPaths)
            {
                string assemblyFullPath = Path.Combine(path, reference.Name) + ".dll";
                if (File.Exists(assemblyFullPath))
                {
                    res = true;
                    break;
                }
            }

            return res;
        }

        static string getAssemblyFullPath(AssemblyName reference)
        {
            string res = "";
            String assemblyName = reference.Name;
            bool hasExtention = reference.Name.EndsWith(".dll") || reference.Name.EndsWith(".exe");

            if (hasExtention == false) assemblyName += ".dll";

            foreach (var path in assemblySearchPaths)
            {
                string assemblyFullPath = Path.Combine(path, assemblyName);
                if (File.Exists(assemblyFullPath))
                {
                    res = assemblyFullPath;
                    break;
                }
            }

            return res;
        }

        static string getAssemblyFullPath(string reference)
        {
            string res = "";
            String assemblyName = reference;
            bool hasExtention = reference.EndsWith(".dll") || reference.EndsWith(".exe");

            if (hasExtention == false) assemblyName += ".dll";

            foreach (var path in assemblySearchPaths)
            {
                string assemblyFullPath = Path.Combine(path, assemblyName);
                if (File.Exists(assemblyFullPath))
                {
                    res = assemblyFullPath;
                    break;
                }
            }

            return res;
        }

        static void resolveReferenceAssemblies(AssemblyName reference)
        {

            String assemblyName = reference.Name;
            bool hasExtention = reference.Name.EndsWith(".dll") || reference.Name.EndsWith(".exe");
            if (hasExtention == false) assemblyName += ".dll";

            string assemblyFullPath = getAssemblyFullPath(assemblyName);

            if (assemblyFullPath != "" && File.Exists(assemblyFullPath))
            {

                File.Copy(assemblyFullPath, Path.Combine(destAssemblyPath, assemblyName), true);

                if (reference.Name == "mscorlib") return;

                try
                {
                    Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFullPath);
                    if (assembly != null)
                    {
                        AssemblyName[] reff = assembly.GetReferencedAssemblies();
                        foreach (AssemblyName refe in reff)
                        {
                            if (referenceExist(refe))
                            {
                                String name = assembliesList.FindLast(item => item.Equals(refe.Name));
                                if (name == null)
                                {
                                    if(verbose) Console.WriteLine(refe.Name);
                                    assembliesList.Add(refe.Name);
                                    resolveReferenceAssemblies(refe);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                       Console.WriteLine(e);
                }
            }
        }

        static void Main(string[] args)
        {

      
            Parser.Default.ParseArguments<Options>(args)
           .WithParsed(RunOptions);

            assemblyPath = Path.GetFullPath(assemblyPath);
            Console.WriteLine("assemblyPath =  " +assemblyPath);

            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            AssemblyName[] reff = assembly.GetReferencedAssemblies();

            foreach (AssemblyName reference in reff)
            {
                if (verbose)Console.WriteLine(reference.Name);
                assembliesList.Add(reference.Name);
                resolveReferenceAssemblies(reference);
            }
        }

        static void RunOptions(Options opts)
        {
            verbose = opts.Verbose;

            if (opts.OutputFolder != "")
            {
                Directory.CreateDirectory(opts.OutputFolder);

                if (Directory.Exists(opts.OutputFolder))
                {
                    destAssemblyPath = opts.OutputFolder;
                }
                else
                {
                    Console.WriteLine("Error Output folder" + opts.OutputFolder + " doesn't exist!!");
                    Environment.Exit(-1);
                }
            }

            if(opts.AssemblyPath != "")
            {
                if(File.Exists(opts.AssemblyPath))
                {
                    assemblyPath = opts.AssemblyPath;
                }
                else
                {
                    Console.WriteLine("Error assembly " + opts.AssemblyPath + " doesn't exist!!");
                    Environment.Exit(-1);
                }
            }

            if(opts.searchPaths != "")
            {
                String [] paths =  opts.searchPaths.Split(",");
                for(int i = 0; i < paths.Length; i++)
                {
                    paths[i] = Path.GetFullPath(paths[i]);
                    Console.WriteLine("search path =  " + paths[i]);
                }
                assemblySearchPaths = paths;
            }
        }



    }
}
