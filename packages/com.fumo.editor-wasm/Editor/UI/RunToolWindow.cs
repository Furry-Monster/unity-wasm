using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Lightweight popup for selecting and running a WASM tool.
    /// Unity does not support dynamic GenericMenu from MenuItem callbacks; this window is the supported alternative.
    /// </summary>
    public sealed class RunToolWindow : EditorWindow
    {
        const float Width = 280f;
        const float RowHeight = 26f;
        const float ChromeHeight = 16f;

        VisualElement _list;

        public static void ShowWindow()
        {
            WasmEditorRuntime.EnsureReady();

            var window = CreateInstance<RunToolWindow>();
            window.titleContent = new GUIContent("Run Tool");
            window.minSize = new Vector2(Width, 80);

            var toolCount = Mathf.Max(WasmEditorRuntime.Tools.Count, 1);
            var height = toolCount * RowHeight + ChromeHeight;
            window.position = CenterOnMainWindow(Width, height);
            window.ShowPopup();
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingTop = 6;
            root.style.paddingBottom = 6;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;

            _list = new VisualElement();
            root.Add(_list);

            WasmEditorRuntime.ToolsChanged += RebuildList;
            RebuildList();
        }

        void OnDestroy()
        {
            WasmEditorRuntime.ToolsChanged -= RebuildList;
        }

        void RebuildList()
        {
            if (_list == null)
                return;

            _list.Clear();

            if (WasmEditorRuntime.Tools.Count == 0)
            {
                _list.Add(new Label("(No tools — Refresh Tools)") { style = { color = Color.gray } });
                return;
            }

            foreach (var tool in WasmEditorRuntime.OrderedTools)
            {
                var toolId = tool.id;
                var button = new Button(() =>
                {
                    WasmEditorRuntime.InvokeTool(toolId);
                    Close();
                })
                {
                    text = tool.name,
                    style = { height = RowHeight, unityTextAlign = TextAnchor.MiddleLeft }
                };
                _list.Add(button);
            }
        }

        static Rect CenterOnMainWindow(float width, float height)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            return new Rect(
                main.x + (main.width - width) * 0.5f,
                main.y + 80f,
                width,
                height);
        }
    }
}
