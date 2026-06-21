using System;
using System.Collections.Generic;
using System.IO;
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
                ToolWindowShell.Instance.SetStatus($"Reloaded {manifest.name}");
                ToolWindowShell.Instance.AppendLog($"Hot reload: {manifest.id}");
            };
            _hotReload.ToolTrapped += (manifest, report) =>
            {
                ToolWindowShell.Instance.ShowTrap(report);
                ToolWindowShell.Instance.SetStatus($"Trap in {manifest.name}");
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
        }

        public static void InvokeTool(string toolId)
        {
            var manifest = _tools.Find(t => t.id == toolId);
            if (manifest == null)
            {
                Debug.LogError($"[WasmEditor] Tool '{toolId}' not found.");
                return;
            }

            ToolWindowShell.Instance.SetStatus($"Running {manifest.name}");
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

    /// <summary>
    /// Dynamically registers menu items for discovered tools.
    /// </summary>
    [InitializeOnLoad]
    public static class ToolMenuRegistry
    {
        static readonly Dictionary<string, ToolManifest> _registered = new();

        static ToolMenuRegistry()
        {
            EditorApplication.delayCall += RebuildMenus;
        }

        public static void RebuildMenus()
        {
            _registered.Clear();
            foreach (var tool in WasmEditorRuntime.Tools)
            {
                if (string.IsNullOrEmpty(tool.menu))
                    continue;

                _registered[tool.menu] = tool;
                // Static MenuItem attributes cannot be added at runtime; use default root menu.
            }
        }

        [MenuItem("Tools/Wasm Editor/Run/Selection Logger", priority = 100)]
        public static void RunSelectionLogger() => WasmEditorRuntime.InvokeTool("com.fumo.selection-logger");
    }
}
