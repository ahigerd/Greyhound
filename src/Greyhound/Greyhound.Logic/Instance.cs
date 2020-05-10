using PhilLibX.IO;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;

namespace Greyhound.Logic
{
    public static class Instance
    {
        /// <summary>
        /// Gets the Supported Package
        /// </summary>
        public static List<IPackage> SupportedPackages { get; } = new List<IPackage>();

        /// <summary>
        /// Gets the Loaded Packages
        /// </summary>
        public static List<IPackage> LoadedPackages { get; } = new List<IPackage>();

        public static Settings GetSettings { get; } = new Settings("Data\\Greyhound.cfg");

        public static string ExportDirectory { get { return "exported_files"; } }

        /// <summary>
        /// Gets or Sets whether or not to skip existing images
        /// </summary>
        public static bool SkipExistingImages { get; set; }

        /// <summary>
        /// Gets or Sets whether or not to skip existing models
        /// </summary>
        public static bool SkipExistingModels { get; set; }

        /// <summary>
        /// Gets or Sets whether or not to skip existing anims
        /// </summary>
        public static bool SkipExistingAnims { get; set; }

        /// <summary>
        /// Gets or Sets whether or not to skip existing sounds
        /// </summary>
        public static bool SkipExistingSounds { get; set; }

        /// <summary>
        /// Gets or Sets the Log Writer
        /// </summary>
        public static StreamWriter LogWriter { get; set; }

        public static List<IModelFormatWriter> ModelWriters { get; } = new List<IModelFormatWriter>();

        public static int ExportThreadCount => int.TryParse(GetSettings["ExportThreadCount", Environment.ProcessorCount.ToString()], out var f) ? f : Environment.ProcessorCount;

        static void LoadImporters(string folder)
        {
            try
            {
                foreach (var script in Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
                {
                    if (CompileCode(File.ReadAllText(script)) is Assembly assembly)
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            if (typeof(IModelFormatWriter).IsAssignableFrom(type))
                            {
                                ModelWriters.Add(Activator.CreateInstance(type) as IModelFormatWriter);
                            }
                        }
                    }
                }
            }
            catch
            {

            }
        }

        static Assembly CompileCode(string code)
        {
            var provider = new Microsoft.CSharp.CSharpCodeProvider();

            CompilerParameters options = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };

            options.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            options.ReferencedAssemblies.Add("Greyhound.Logic.dll");
            options.ReferencedAssemblies.Add("System.Linq.dll");
            options.ReferencedAssemblies.Add("PhilLibX.dll");
            options.ReferencedAssemblies.Add("System.Numerics.dll");

            var result = provider.CompileAssemblyFromSource(options, code);

            if (result.Errors.HasErrors)
            {
                Console.WriteLine(result.Errors[0]);
                // TODO: error reporting
                return null;
            }

            if (result.Errors.HasWarnings)
            {
                // TODO: error reporting
            }

            return result.CompiledAssembly;
        }

        public static void BuildTypeList<T>(List<T> list)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(var type in assembly.GetTypes())
                {
                    if(!type.IsInterface && !type.IsAbstract && typeof(T).IsAssignableFrom(type))
                    {
                        list.Add((T)Activator.CreateInstance(type));
                    }
                }
            }
        }

        static void InitLog()
        {
            try
            {
                LogWriter = new StreamWriter("Log.txt", true);
            }
            catch
            {
                LogWriter?.Dispose();
            }
        }

        static Instance()
        {
            LoadImporters("Data\\Plugins");



            BuildTypeList(SupportedPackages);
            BuildTypeList(ModelWriters);

            InitLog();
        }

        public static void ClearPackages()
        {
            foreach (var obj in LoadedPackages)
                obj.Dispose();

            LoadedPackages.Clear();
        }

        /// <summary>
        /// Gets the Package for the given stream with the given extension
        /// </summary>
        /// <param name="stream">Stream to check</param>
        /// <param name="extension">Extension to check</param>
        /// <returns>Package object if found, otherwise null</returns>
        public static IPackage GetPackageClass(Stream stream, string extension)
        {
            // Read the first 16 bytes if we need further verification (atm just fast files)
            byte[] buffer = new byte[16];
            stream.Read(buffer, 0, buffer.Length);
            stream.Seek(0, SeekOrigin.Begin);

            foreach (var package in SupportedPackages)
                if (package.IsFileValid(stream, buffer, extension))
                    return package.CreateNew();

            return null;
        }

        public static byte[] ExtractPackageEntry(object data, int size)
        {
            // If we have the literal entry, lesssss gooooo
            if (data is PackageEntry entry)
                return entry.BasePackage.Extract(entry, size);

            var key = data.ToString();

            // no, we have a key, we must delve deeper
            foreach (var package in LoadedPackages)
                if (package.Entries.TryGetValue(key, out var result))
                    return result.BasePackage.Extract(result, size);

            // anal probe failed
            return null;
        }

        /// <summary>
        /// Performs a deep search to find package entry, if not found, null is returned
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static PackageEntry FindPackageEntry(object data)
        {
            foreach (var package in LoadedPackages)
            {
                foreach(var entry in package.Entries)
                {
                    if(entry.Key.Contains(data.ToString()))
                    {
                        return entry.Value;
                    }
                }
            }
            
            return null;
        }

        public static PackageEntry FindPackage(object data)
        {
            foreach (var package in LoadedPackages)
            {
                foreach (var entry in package.Entries)
                {
                    if (entry.Key.Contains(data.ToString()))
                    {
                        return entry.Value;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Writes to the log
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public static void Log(string value, string messageType)
        {
            if(LogWriter != null)
            {
                lock (LogWriter)
                {
                    LogWriter.WriteLine("{0} [ {1} ] {2}", DateTime.Now.ToString("dd-MM-yyyy - HH:mm:ss"), messageType.PadRight(12), value);
                    LogWriter.Flush();
                }
            }
        }
    }
}
