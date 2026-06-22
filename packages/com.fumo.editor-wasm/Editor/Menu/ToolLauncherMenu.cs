using UnityEditor;
using UnityEngine;

namespace Fumo.EditorWasm
{
    public static class ToolLauncherMenu
    {
        [MenuItem("Tools/Wasm Editor/Run Tool...", priority = 10)]
        public static void ShowRunToolMenu()
        {
            RunToolWindow.ShowWindow(GetActivatorScreenPosition());
        }

        static Vector2 GetActivatorScreenPosition()
        {
            if (Event.current != null)
                return GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

            var main = EditorGUIUtility.GetMainWindowPosition();
            return new Vector2(main.x + 160, main.y + 28);
        }
    }
}
