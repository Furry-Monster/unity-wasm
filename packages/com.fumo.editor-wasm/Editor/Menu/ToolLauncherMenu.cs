using System.Linq;
using UnityEditor;

namespace Fumo.EditorWasm
{
    public static class ToolLauncherMenu
    {
        [MenuItem("Tools/Wasm Editor/Run Tool...", priority = 10)]
        public static void ShowRunToolMenu()
        {
            var menu = new GenericMenu();
            foreach (var tool in WasmEditorRuntime.Tools.OrderBy(t => t.name))
            {
                var toolId = tool.id;
                menu.AddItem(new GUIContent(tool.name), false, () => WasmEditorRuntime.InvokeTool(toolId));
            }

            if (WasmEditorRuntime.Tools.Count == 0)
                menu.AddDisabledItem(new GUIContent("(No tools — Refresh Tools)"));

            menu.ShowAsContext();
        }
    }
}
