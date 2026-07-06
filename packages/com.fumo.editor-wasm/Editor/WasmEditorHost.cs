using System;
using System.IO;
using UnityEngine;
using Wasmtime;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Loads, executes, and hot-reloads WASM editor tools via Wasmtime.
    /// </summary>
    public sealed class WasmEditorHost : IDisposable
    {
        public const ulong DefaultFuel = 50_000_000;
        public const long DefaultMemoryLimitBytes = 256L * 1024 * 1024;

        readonly Engine _engine;
        readonly EditorHostBridge _bridge;
        readonly HostCallTrace _trace = new();

        Store _store;
        Linker _linker;
        Module _module;
        Instance _instance;
        Memory _guestMemory;
        ToolManifest _manifest;
        bool _disposed;

        public ToolManifest Manifest => _manifest;
        public HostCallTrace Trace => _trace;
        public EditorHostBridge Bridge => _bridge;
        public TrapReport LastTrapReport { get; private set; }
        public ModuleInspect ModuleInspect => ModuleInspect.FromModule(_module);
        public bool IsLoaded => _instance != null;

        public WasmEditorHost(bool debugInfo = true)
        {
            var config = new Config()
                .WithDebugInfo(debugInfo)
                .WithOptimizationLevel(OptimizationLevel.Speed)
                .WithFuelConsumption(true);

            _engine = new Engine(config);
            _bridge = new EditorHostBridge(_trace);
        }

        public void Load(ToolManifest manifest, byte[] wasmBytes)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            if (wasmBytes == null || wasmBytes.Length == 0)
                throw new ArgumentException("WASM bytecode is empty.", nameof(wasmBytes));

            AbiVersion.ValidateForLoad(manifest);

            var candidate = Module.FromBytes(_engine, manifest.id ?? "tool", wasmBytes);
            if (!HostImportRegistryRuntime.ValidateGuestImports(candidate, out var importError))
                throw new InvalidOperationException(importError);
            if (!HostImportRegistryRuntime.ValidateGuestExports(candidate, manifest, out var exportError))
                throw new InvalidOperationException(exportError);

            Unload();

            _manifest = manifest;
            _store = new Store(_engine);
            _store.SetLimits(memorySize: DefaultMemoryLimitBytes);
            _store.Fuel = DefaultFuel;

            _linker = new Linker(_engine);
            _bridge.ClearHandles();
            _bridge.SetGuestMemory(null);
            _trace.Clear();
            _bridge.RegisterImports(_linker);

            _module = candidate;
            _instance = _linker.Instantiate(_store, _module);
            _guestMemory = _instance.GetMemory("memory");
            _bridge.SetGuestMemory(_guestMemory);

            CallExport(manifest.exports.on_init, optional: false);
        }

        public void LoadFromManifest(ToolManifest manifest)
        {
            var bytes = File.ReadAllBytes(manifest.WasmPath);
            Load(manifest, bytes);
        }

        public void Reload(byte[] wasmBytes) => Load(_manifest, wasmBytes);

        public void ReloadFromDisk()
        {
            if (_manifest == null)
                throw new InvalidOperationException("No tool manifest loaded.");
            LoadFromManifest(_manifest);
        }

        public void CallExport(string exportName, bool optional = false)
        {
            if (string.IsNullOrEmpty(exportName))
                return;

            if (_instance == null)
                throw new InvalidOperationException("No WASM instance loaded.");

            try
            {
                if (!TryInvokeExport(exportName))
                {
                    if (optional)
                        return;
                    throw new MissingMethodException($"Export '{exportName}' not found in WASM module.");
                }

                LastTrapReport = null;
            }
            catch (TrapException ex)
            {
                LastTrapReport = TrapReport.FromException(
                    _manifest?.id ?? "unknown",
                    exportName,
                    ex,
                    _store?.Fuel ?? 0,
                    _trace);
                Debug.LogError($"[WasmEditor] Trap in '{exportName}':\n{LastTrapReport.ToJson()}");
                throw;
            }
        }

        bool TryInvokeExport(string exportName)
        {
            var action = _instance.GetAction(exportName);
            if (action != null)
            {
                action();
                return true;
            }

            var funcInt = _instance.GetFunction<int>(exportName);
            if (funcInt != null)
            {
                var code = funcInt();
                if (code != 0)
                    Debug.LogWarning($"[WasmEditor] Export '{exportName}' returned non-zero status {code}.");
                return true;
            }

            return false;
        }

        public void Shutdown()
        {
            if (_instance == null)
                return;

            try
            {
                CallExport(_manifest?.exports?.on_shutdown, optional: true);
            }
            catch (TrapException)
            {
                // Already logged in CallExport.
            }
        }

        public void Unload()
        {
            Shutdown();

            _instance = null;
            _module = null;
            _guestMemory = null;
            _bridge.SetGuestMemory(null);
            _bridge.ClearHandles();

            _linker?.Dispose();
            _linker = null;

            _store?.Dispose();
            _store = null;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Unload();
            _engine.Dispose();
            _disposed = true;
        }
    }
}
