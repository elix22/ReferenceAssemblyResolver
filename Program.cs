using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security;
using CommandLine;

namespace ReferenceAssemblyResolver
{

 

    public static class ShellHelper
    {

        static string monodis_exe = "";


        static bool FindMonoDisInDirectory(string path)
        {
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isOSX = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            bool isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux);


            string subFolder = "";
            if (isWindows == true) { subFolder =  "windows"; }
            if (isOSX == true) { subFolder = "macos"; }
            if (isLinux == true) { subFolder = "linux"; }

            string exe_name = "monodis";
            if(isWindows) exe_name += ".exe";

            string [] directories =  Directory.GetDirectories(path, subFolder, SearchOption.AllDirectories);

            foreach (string directory in directories)
            {
                string[] fileEntries = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                foreach (string fileName in fileEntries)
                {
                    if (Path.GetFileName(fileName) == exe_name)
                    {
                        monodis_exe = Path.GetFullPath(fileName);
                        Console.WriteLine("monodis path : " + monodis_exe);
                        return true;
                    }
                }
            }
            
            return false;
        }

        static bool FindMonoDisExe()
        {
            string cwd = Directory.GetCurrentDirectory();
            //  Console.WriteLine("cwd : " + cwd);
            string searchDirectory = cwd;

            while (Directory.Exists(searchDirectory))
            {
                if (FindMonoDisInDirectory(searchDirectory) == true) return true;
                searchDirectory = Path.Combine(searchDirectory, "..");
            }
               
            return false;
        }

        public static string[] GetReferencedAssemblies(this string cmd)
        {


            if(monodis_exe == "")
            {
                FindMonoDisExe();
            }

            if(monodis_exe == "")
            {
                Console.WriteLine("Didn't find monodis");
            }

            monodis_exe = Path.GetFullPath(monodis_exe);

           // Console.WriteLine("monodis path : " + monodis_exe);

            var escapedArgs = cmd.Replace("\"", "\\\"");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = monodis_exe,
                    Arguments = "--assemblyref " + cmd,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

          
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string [] entries = result.Split("\n");
            List<string>  reference_assemblies = new List<string>();

            foreach(string entry in entries)
            {
                if(entry.Contains("Name="))
                {
                    String name = entry.Replace("Name=", "").Replace("\r", "").Replace("\t", "");
                    reference_assemblies.Add(name);
                }
            }

            return reference_assemblies.ToArray();
        }

    }

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
     * ReferenceAssemblyResolver.exe   --output "E:/Development/ReferenceAssemblyResolver/temp2" --assembly "E:\Development\TestProject\bin\Debug\netcoreapp3.1\Game.dll" --search "E:\Development\TestProject\bin\Debug\netcoreapp3.1,E:\Development\TestProject\libs\dotnet\bcl\android,E:\Development\TestProject\libs\dotnet\bcl\android\Facades"
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

        static bool referenceExist(string Name)
        {
            bool res = false;
            bool hasExtention = Name.EndsWith(".dll") || Name.EndsWith(".exe");

            if (hasExtention == false) Name += ".dll";


            foreach (var path in assemblySearchPaths)
            {
                string assemblyFullPath = Path.Combine(path, Name);
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
                                    if(verbose) Console.WriteLine("adding reference : " + refe.Name);
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


        static void resolveReferenceAssemblies(String assemblyName)
        {

            bool hasExtention = assemblyName.EndsWith(".dll") || assemblyName.EndsWith(".exe");
            if (hasExtention == false) assemblyName += ".dll";

            string assemblyFullPath = getAssemblyFullPath(assemblyName);

            if (assemblyFullPath != "" && File.Exists(assemblyFullPath))
            {

                File.Copy(assemblyFullPath, Path.Combine(destAssemblyPath, assemblyName), true);

                if (assemblyName == "mscorlib.dll") return;

                string[] reff = ShellHelper.GetReferencedAssemblies(assemblyFullPath);
                foreach (string refe in reff)
                {
                    if (referenceExist(refe))
                    {
                        String name = assembliesList.FindLast(item => item.Equals(refe));
                        if (name == null)
                        {
                            if (verbose) Console.WriteLine("adding reference : " + refe);
                            assembliesList.Add(refe);
                            resolveReferenceAssemblies(refe);
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args)
           .WithParsed(RunOptions);

            string cwd = Directory.GetCurrentDirectory();
            Console.WriteLine("cwd : "+cwd);

            assemblyPath = Path.GetFullPath(assemblyPath);
            Console.WriteLine("assemblyPath =  " +assemblyPath);

            string[] reff = ShellHelper.GetReferencedAssemblies(assemblyPath);

            foreach (string reference in reff)
            {
                if (verbose)Console.WriteLine("adding reference : " + reference);
                assembliesList.Add(reference);
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
