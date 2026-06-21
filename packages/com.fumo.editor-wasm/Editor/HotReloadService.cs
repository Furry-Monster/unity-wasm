using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Watches tool.wasm files and hot-reloads the associated host instance.
    /// </summary>
    public sealed class HotReloadService : IDisposable
    {
        readonly Dictionary<string, FileSystemWatcher> _watchers = new();
        readonly Dictionary<string, DateTime> _pendingReload = new();
        readonly Dictionary<string, WasmEditorHost> _hosts = new();
        readonly Dictionary<string, ToolManifest> _manifests = new();

        bool _disposed;
        double _lastPollTime;

        public event Action<ToolManifest> ToolReloaded;
        public event Action<ToolManifest, TrapReport> ToolTrapped;

        public IReadOnlyDictionary<string, WasmEditorHost> Hosts => _hosts;

        public void Register(ToolManifest manifest)
        {
            if (manifest == null || string.IsNullOrEmpty(manifest.id))
                return;

            _manifests[manifest.id] = manifest;
            EnsureHost(manifest);
            Watch(manifest);
        }

        public void RegisterAll(IEnumerable<ToolManifest> manifests)
        {
            foreach (var manifest in manifests)
                Register(manifest);
        }

        public WasmEditorHost GetHost(string toolId)
        {
            _hosts.TryGetValue(toolId, out var host);
            return host;
        }

        public void InvokeMenu(ToolManifest manifest)
        {
            var host = EnsureHost(manifest);
            try
            {
                host.CallExport(manifest.exports.on_menu_click);
            }
            catch (Wasmtime.TrapException)
            {
                ToolTrapped?.Invoke(manifest, host.LastTrapReport);
            }
        }

        public void Poll()
        {
            if (_pendingReload.Count == 0)
                return;

            var now = EditorApplication.timeSinceStartup;
            if (now - _lastPollTime < 0.3f)
                return;
            _lastPollTime = now;

            var due = new List<string>();
            foreach (var pair in _pendingReload)
            {
                if (DateTime.UtcNow - pair.Value >= TimeSpan.FromMilliseconds(300))
                    due.Add(pair.Key);
            }

            foreach (var toolId in due)
            {
                _pendingReload.Remove(toolId);
                if (!_manifests.TryGetValue(toolId, out var manifest))
                    continue;

                try
                {
                    var host = EnsureHost(manifest);
                    host.ReloadFromDisk();
                    ToolReloaded?.Invoke(manifest);
                    Debug.Log($"[WasmEditor] Hot-reloaded '{manifest.name}' ({manifest.id})");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WasmEditor] Hot reload failed for '{toolId}': {ex.Message}");
                }
            }
        }

        WasmEditorHost EnsureHost(ToolManifest manifest)
        {
            if (_hosts.TryGetValue(manifest.id, out var existing))
                return existing;

            var host = new WasmEditorHost(debugInfo: true);
            host.LoadFromManifest(manifest);
            _hosts[manifest.id] = host;
            return host;
        }

        void Watch(ToolManifest manifest)
        {
            var wasmPath = manifest.WasmPath;
            var dir = Path.GetDirectoryName(wasmPath);
            var file = Path.GetFileName(wasmPath);
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file))
                return;

            if (_watchers.ContainsKey(manifest.id))
                return;

            var watcher = new FileSystemWatcher(dir, file)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            FileSystemEventHandler handler = (_, __) => _pendingReload[manifest.id] = DateTime.UtcNow;
            watcher.Changed += handler;
            watcher.Created += handler;
            watcher.Renamed += (_, __) => _pendingReload[manifest.id] = DateTime.UtcNow;

            _watchers[manifest.id] = watcher;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            _watchers.Clear();

            foreach (var host in _hosts.Values)
                host.Dispose();
            _hosts.Clear();
            _manifests.Clear();
            _pendingReload.Clear();
            _disposed = true;
        }
    }
}
