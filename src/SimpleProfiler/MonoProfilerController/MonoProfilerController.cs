using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using Common;
using UnityEngine;

namespace MonoProfiler
{
    [BepInPlugin(GUID, "MonoProfiler Controller", Version)]
    public class MonoProfilerController : BaseUnityPlugin
    {
        public const string GUID = "MonoProfiler";
        public const string Version = Metadata.Version;

        private ConfigEntry<KeyboardShortcut> _key;

        private void Awake()
        {
            System.Console.WriteLine($"[C#] Initializing MonoProfilerController");
            if (!MonoProfilerPatcher.IsInitialized)
            {
                enabled = false;
                Logger.LogWarning("MonoProfiler was not initialized, can't proceed! Make sure that all profiler dlls are in the correct directories.");
                return;
            }

            _key = Config.Bind("Capture", "Dump collected data", new KeyboardShortcut(KeyCode.F6), "Key used to dump all information to a file. Only includes information that was captured since the last time a dump was triggered.");
        }

        private void Update()
        {
            if (!_key.Value.IsDown())
                return;

            var dumpFile = MonoProfilerPatcher.RunProfilerDump();
            var containingDirectory = dumpFile.DirectoryName ?? throw new InvalidOperationException("dumpFile.DirectoryName is null for " + dumpFile);
            dumpFile.MoveTo(Path.Combine(containingDirectory, $"{Path.GetFileNameWithoutExtension(dumpFile.Name)}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{dumpFile.Extension}"));
            Logger.LogMessage("  Saved profiler data to " + dumpFile.FullName);
        }
    }
}
