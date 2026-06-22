using Fumo.EditorWasm.Generator;
using UnityEditor;

namespace Fumo.EditorWasm
{
    public static class UnityWasmMenu
    {
        [MenuItem("Tools/Wasm Editor/Refresh Tools", priority = 0)]
        public static void RefreshTools() => WasmEditorRuntime.RefreshTools();

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
