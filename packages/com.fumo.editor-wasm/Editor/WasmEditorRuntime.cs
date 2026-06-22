using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Editor orchestration: discovery, hot reload, and tool invocation.
    /// </summary>
    [InitializeOnLoad]
    public static class WasmEditorRuntime
    {
        static HotReloadService _hotReload;
        static readonly List<ToolManifest> _tools = new();

        public static event Action ToolsChanged;
        public static event Action Initialized;

        public static HotReloadService HotReload => _hotReload;
        public static IReadOnlyList<ToolManifest> Tools => _tools;
        public static IEnumerable<ToolManifest> OrderedTools => _tools.OrderBy(t => t.name);

        static WasmEditorRuntime()
        {
            EditorApplication.delayCall += Initialize;
            EditorApplication.update += OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        }

        public static void EnsureReady()
        {
            if (_hotReload == null)
                Initialize();
        }

        static void Initialize()
        {
            if (_hotReload != null)
                return;

            _hotReload = new HotReloadService();
            _hotReload.ToolReloaded += manifest =>
            {
                ToolWindowShell.NotifyStatus($"Reloaded {manifest.name}");
                ToolWindowShell.NotifyLog($"Hot reload: {manifest.id}");
            };
            _hotReload.ToolTrapped += (manifest, report) =>
            {
                ToolWindowShell.NotifyTrap(report);
                ToolWindowShell.NotifyStatus($"Trap in {manifest.name}");
            };

            RefreshTools();
            Initialized?.Invoke();
        }

        static void OnUpdate() => _hotReload?.Poll();

        public static void RefreshTools()
        {
            EnsureReady();
            _tools.Clear();
            _tools.AddRange(ToolDiscoveryService.DiscoverAll());
            _hotReload.RegisterAll(_tools);
            Debug.Log($"[WasmEditor] Discovered {_tools.Count} tool(s).");
            ToolsChanged?.Invoke();
        }

        public static void InvokeTool(string toolId)
        {
            EnsureReady();

            var manifest = _tools.Find(t => t.id == toolId);
            if (manifest == null)
            {
                Debug.LogError($"[WasmEditor] Tool '{toolId}' not found.");
                return;
            }

            ToolWindowShell.NotifyStatus($"Running {manifest.name}");
            _hotReload.InvokeMenu(manifest);
        }

        static void Dispose()
        {
            _hotReload?.Dispose();
            _hotReload = null;
            _tools.Clear();
        }
    }
}
