using UnityEditor;

namespace Fumo.EditorWasm
{
    public static class ToolLauncherMenu
    {
        [MenuItem("Tools/Wasm Editor/Run Tool...", priority = 10)]
        public static void ShowRunToolMenu() => RunToolWindow.ShowWindow();
    }
}
