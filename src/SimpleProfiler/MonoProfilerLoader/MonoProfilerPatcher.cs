using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;

namespace MonoProfiler
{
    public static class MonoProfilerPatcher
    {
        private const string ProfilerOutputFilename = "MonoProfilerOutput.csv";
        private static Dump _dumpFunction;
        private static ManualLogSource _logger;

        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        private static bool Is64BitProcess => IntPtr.Size == 8;
        public static bool IsInitialized => _dumpFunction != null;

        public static FileInfo RunProfilerDump()
        {
            if (_dumpFunction == null) throw new InvalidOperationException("Tried to trigger a profiler info dump before profiler was initialized");

            _dumpFunction();

            var dump = new FileInfo(Path.Combine(Paths.GameRootPath, ProfilerOutputFilename));
            if (!dump.Exists) throw new FileNotFoundException("Could not find the profiler dump file in " + dump.FullName);
            return dump;
        }

        public static void Initialize()
        {
            _logger = new ManualLogSource("MonoProfiler");
            Logger.Sources.Add(_logger);

            try
            {
                // Find address of the mono module
                // var monoModules = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Where(module => module.ModuleName.Contains("mono"));
                // foreach (ProcessModule module in monoModules)
                // {
                //     System.Console.WriteLine($"    found module {module.FileName} == {module.ModuleName}");
                // }
                // ProcessModule monoModule = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().FirstOrDefault(module => module.ModuleName.Contains("libmonosgen"));
                ProcessModule monoModule = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().FirstOrDefault(module => module.ModuleName.Contains("libmono.so"));
                if (monoModule == null)
                {
                    _logger.LogError("Failed to find the Mono module in current process");
                    return;
                }
                System.Console.WriteLine($"  [!] got mono module {monoModule.FileName}");

                System.Console.WriteLine($"  [!] looking for profiler");
                // Load profiler lib, it checks for the dll in the game root next to the .exe first
                var profilerPath = Path.GetFullPath(Path.Combine(Paths.GameRootPath, "MonoProfiler64.so"));
                // var profilerPath = Path.GetFullPath(Path.Combine(Paths.GameRootPath, Is64BitProcess ? "MonoProfiler64.dll" : "MonoProfiler32.dll"));
                if(!File.Exists(profilerPath))
                {
                    _logger.LogError($"Could not find {profilerPath}");
                    return;
                }
                // var profilerPtr = LoadLibrary(profilerPath);
                System.Console.WriteLine($"  [!] opening profiler");
                IntPtr profilerPtr = dlopen(profilerPath, RTLD_NOW);
                if (profilerPtr == IntPtr.Zero)
                {
                    _logger.LogError($"Failed to load {profilerPath}, verify that the file exists and is not corrupted");
                    return;
                }
                System.Console.WriteLine($"  [!] got profiler {profilerPtr}");

                // Subscribe the profiler in mono
                // var addProfilerPtr = GetProcAddress(profilerPtr, "AddProfiler");
                System.Console.WriteLine($"  [!] loading AddProfiler() method");
                IntPtr addProfilerPtr = dlsym(profilerPtr, "AddProfiler");
                if (addProfilerPtr == IntPtr.Zero)
                {
                    _logger.LogError("Failed to find function AddProfiler in MonoProfiler.dll");
                    return;
                }
                System.Console.WriteLine($"  [!] marshalling AddProfiler() method at {addProfilerPtr}");
                var addProfiler = (AddProfilerDelegate)Marshal.GetDelegateForFunctionPointer(addProfilerPtr, typeof(AddProfilerDelegate));
                System.Console.WriteLine($"  [!] calling AddProfiler() method");
                addProfiler(monoModule.BaseAddress);
                System.Console.WriteLine($"  [!] done with AddProfiler() method");

                // Prepare callback used to trigger a dump of collected profiler info
                // var dumpPtr = GetProcAddress(profilerPtr, "Dump");
                IntPtr dumpPtr = dlsym(profilerPtr, "Dump");
                if (dumpPtr == IntPtr.Zero)
                {
                    _logger.LogError("Failed to find function Dump in MonoProfiler.dll");
                    return;
                }
                _dumpFunction = (Dump)Marshal.GetDelegateForFunctionPointer(dumpPtr, typeof(Dump));

                _logger.LogDebug($"Loaded profiler from {profilerPath}"); // monoModule:{monoModule} profilerPtr:{profilerPtr} AddProfilerPtr:{addProfilerPtr} DumpPtr:{dumpPtr}
            }
            catch (Exception ex)
            {
                _logger.LogError("Encountered an unexpected exception: " + ex);
            }
            finally
            {
                Logger.Sources.Remove(_logger);
            }
        }

        public static void Patch(AssemblyDefinition ass) { }

        // [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        // private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        // [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        // private static extern IntPtr LoadLibrary(string fileName);

        // https://stackoverflow.com/questions/13461989/p-invoke-to-dynamically-loaded-library-on-mono
        [DllImport("/usr/lib/libdl.so.2" /*"libdl.so"*/)]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport("/usr/lib/libdl.so.2" /*"libdl.so"*/)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        // const int RTLD_LAZY = 1; // for dlopen's flags
        const int RTLD_NOW = 2; // for dlopen's flags

        private delegate void AddProfilerDelegate(IntPtr mono);
        private delegate void Dump();
    }
}
