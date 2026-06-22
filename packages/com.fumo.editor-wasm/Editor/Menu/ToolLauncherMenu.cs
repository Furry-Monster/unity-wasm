using UnityEditor;
using UnityEngine;

namespace Fumo.EditorWasm
{
    public static class ToolLauncherMenu
    {
        [MenuItem("Tools/Wasm Editor/Run Tool...", priority = 10)]
        public static void ShowRunToolMenu()
        {
            WasmEditorRuntime.EnsureReady();

            var menu = new GenericMenu();
            WasmEditorRuntime.PopulateRunToolMenu(menu);

            if (Event.current != null)
                menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
            else
                menu.ShowAsContext();
        }
    }
}
