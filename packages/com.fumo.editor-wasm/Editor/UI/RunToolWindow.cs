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
        VisualElement _root;

        public static void ShowWindow(Vector2 activatorScreenPosition)
        {
            WasmEditorRuntime.EnsureReady();

            var window = CreateInstance<RunToolWindow>();
            window.titleContent = new GUIContent("Run Tool");
            window.minSize = new Vector2(Width, 80);

            var toolCount = Mathf.Max(WasmEditorRuntime.Tools.Count, 1);
            var height = toolCount * RowHeight + ChromeHeight;
            window.position = PlaceNearScreenPoint(activatorScreenPosition, Width, height);
            window.ShowPopup();
            window.Focus();
        }

        void CreateGUI()
        {
            _root = rootVisualElement;
            _root.style.paddingTop = 6;
            _root.style.paddingBottom = 6;
            _root.style.paddingLeft = 8;
            _root.style.paddingRight = 8;
            _root.focusable = true;
            _root.RegisterCallback<FocusOutEvent>(OnRootFocusOut);

            _list = new VisualElement();
            _root.Add(_list);

            WasmEditorRuntime.ToolsChanged += RebuildList;
            RebuildList();
            _root.Focus();
        }

        void OnDestroy()
        {
            if (_root != null)
                _root.UnregisterCallback<FocusOutEvent>(OnRootFocusOut);

            WasmEditorRuntime.ToolsChanged -= RebuildList;
        }

        void OnRootFocusOut(FocusOutEvent evt)
        {
            var newFocus = evt.relatedTarget as VisualElement;
            if (newFocus != null && _root.Contains(newFocus))
                return;

            // Close after the focus event finishes; synchronous Close() re-enters HostView.OnLostFocus.
            RequestDismiss();
        }

        void RequestDismiss()
        {
            if (_root == null)
                return;

            _root.schedule.Execute(Dismiss).StartingIn(0);
        }

        void Dismiss()
        {
            if (this != null)
                Close();
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
                    RequestDismiss();
                })
                {
                    text = tool.name,
                    style = { height = RowHeight, unityTextAlign = TextAnchor.MiddleLeft }
                };
                _list.Add(button);
            }
        }

        static Rect PlaceNearScreenPoint(Vector2 screenPoint, float width, float height)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            var x = Mathf.Clamp(screenPoint.x, main.x, main.xMax - width);
            var y = Mathf.Clamp(screenPoint.y, main.y, main.yMax - height);
            return new Rect(x, y, width, height);
        }
    }
}
