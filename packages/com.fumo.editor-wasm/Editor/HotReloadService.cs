using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Wasmtime;

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
        readonly Dictionary<string, DateTime> _lastHotReloadUtc = new();

        bool _disposed;
        double _lastPollTime;

        public event Action<ToolManifest> ToolReloaded;
        public event Action<ToolManifest, TrapReport> ToolTrapped;

        public IReadOnlyDictionary<string, WasmEditorHost> Hosts => _hosts;

        public DateTime? GetLastHotReloadUtc(string toolId)
        {
            if (_lastHotReloadUtc.TryGetValue(toolId, out var utc))
                return utc;
            return null;
        }

        public void Register(ToolManifest manifest)
        {
            if (manifest == null || string.IsNullOrEmpty(manifest.id))
                return;

            _manifests[manifest.id] = manifest;

            if (_hosts.TryGetValue(manifest.id, out var existing))
            {
                if (!string.Equals(existing.Manifest?.WasmPath, manifest.WasmPath, StringComparison.Ordinal))
                    TryRunLoad(manifest, host => host.LoadFromManifest(manifest));
            }
            else
            {
                TryRunLoad(manifest, host => host.LoadFromManifest(manifest));
            }

            EnsureWatching(manifest);
        }

        public void RegisterAll(IEnumerable<ToolManifest> manifests)
        {
            var seen = new HashSet<string>();
            foreach (var manifest in manifests)
            {
                seen.Add(manifest.id);
                Register(manifest);
            }

            var removed = new List<string>();
            foreach (var id in _manifests.Keys)
            {
                if (!seen.Contains(id))
                    removed.Add(id);
            }

            foreach (var id in removed)
                Unregister(id);
        }

        public WasmEditorHost GetHost(string toolId)
        {
            _hosts.TryGetValue(toolId, out var host);
            return host;
        }

        public void InvokeMenu(ToolManifest manifest)
        {
            if (!TryRunLoad(manifest, host =>
                {
                    if (!host.IsLoaded)
                        host.LoadFromManifest(manifest);
                }))
                return;

            if (!_hosts.TryGetValue(manifest.id, out var host))
                return;

            try
            {
                host.CallExport(manifest.exports.on_menu_click);
            }
            catch (TrapException)
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

                if (!TryRunLoad(manifest, host => host.ReloadFromDisk()))
                    continue;

                RecordHotReload(toolId);
                ToolReloaded?.Invoke(manifest);
                Debug.Log($"[WasmEditor] Hot-reloaded '{manifest.name}' ({manifest.id})");
            }
        }

        void RecordHotReload(string toolId) => _lastHotReloadUtc[toolId] = DateTime.UtcNow;

        bool TryRunLoad(ToolManifest manifest, Action<WasmEditorHost> load)
        {
            WasmEditorHost host = null;
            var isNew = false;

            try
            {
                if (!_hosts.TryGetValue(manifest.id, out host))
                {
                    host = new WasmEditorHost(debugInfo: true);
                    isNew = true;
                }

                load(host);

                if (isNew)
                    _hosts[manifest.id] = host;

                return true;
            }
            catch (TrapException)
            {
                ToolTrapped?.Invoke(manifest, host?.LastTrapReport);
                if (isNew && host != null)
                    host.Dispose();
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError(ex.Message);
                ToolWindowShell.NotifyStatus($"Load failed: {manifest.name}");
                ToolWindowShell.NotifyLog(ex.Message);
                if (isNew && host != null)
                    host.Dispose();
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WasmEditor] Load failed for '{manifest.id}': {ex.Message}");
                if (isNew && host != null)
                    host.Dispose();
                return false;
            }
        }

        void EnsureWatching(ToolManifest manifest)
        {
            var wasmPath = manifest.WasmPath;
            var dir = Path.GetDirectoryName(wasmPath);
            var file = Path.GetFileName(wasmPath);
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file))
                return;

            if (_watchers.TryGetValue(manifest.id, out var existing))
            {
                if (string.Equals(existing.Path, dir, StringComparison.Ordinal) &&
                    string.Equals(existing.Filter, file, StringComparison.Ordinal))
                    return;

                StopWatching(manifest.id);
            }

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

        void StopWatching(string toolId)
        {
            if (!_watchers.TryGetValue(toolId, out var watcher))
                return;

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            _watchers.Remove(toolId);
        }

        void Unregister(string toolId)
        {
            StopWatching(toolId);

            if (_hosts.TryGetValue(toolId, out var host))
            {
                host.Dispose();
                _hosts.Remove(toolId);
            }

            _manifests.Remove(toolId);
            _pendingReload.Remove(toolId);
            _lastHotReloadUtc.Remove(toolId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var toolId in new List<string>(_watchers.Keys))
                StopWatching(toolId);

            foreach (var host in _hosts.Values)
                host.Dispose();
            _hosts.Clear();
            _manifests.Clear();
            _pendingReload.Clear();
            _lastHotReloadUtc.Clear();
            _disposed = true;
        }
    }
}
