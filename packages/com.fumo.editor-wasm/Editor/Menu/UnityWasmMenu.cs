using System;
using System.Collections.Generic;
using Fumo.EditorWasm.Generator;
using UnityEditor;
using UnityEngine;

namespace Fumo.EditorWasm
{
    [InitializeOnLoad]
    public static class WasmEditorRuntime
    {
        static HotReloadService _hotReload;
        static readonly List<ToolManifest> _tools = new();

        public static event Action ToolsChanged;

        public static HotReloadService HotReload => _hotReload;
        public static IReadOnlyList<ToolManifest> Tools => _tools;

        static WasmEditorRuntime()
        {
            EditorApplication.delayCall += Initialize;
            EditorApplication.update += OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
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
        }

        static void OnUpdate() => _hotReload?.Poll();

        public static void RefreshTools()
        {
            _tools.Clear();
            _tools.AddRange(ToolDiscoveryService.DiscoverAll());
            _hotReload?.RegisterAll(_tools);
            Debug.Log($"[WasmEditor] Discovered {_tools.Count} tool(s).");
            ToolsChanged?.Invoke();
        }

        public static void InvokeTool(string toolId)
        {
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

    public static class UnityWasmMenu
    {
        [MenuItem("Tools/Wasm Editor/Refresh Tools", priority = 0)]
        public static void RefreshTools()
        {
            WasmEditorRuntime.RefreshTools();
        }

        [MenuItem("Tools/Wasm Editor/Open Tool Shell", priority = 1)]
        public static void OpenShell() => ToolWindowShell.ShowWindow();

        [MenuItem("Tools/Wasm Editor/Export Tool Registry", priority = 22)]
        public static void ExportRegistry() => ToolRegistryExporter.Export();

        [MenuItem("Tools/Wasm Editor/Export API Schema", priority = 20)]
        public static void ExportSchema() => SchemaExporter.ExportToDefaultPath();

        [MenuItem("Tools/Wasm Editor/Generate Host Bindings", priority = 21)]
        public static void GenerateBindings() => HostBindingGenerator.Generate();
    }
}
