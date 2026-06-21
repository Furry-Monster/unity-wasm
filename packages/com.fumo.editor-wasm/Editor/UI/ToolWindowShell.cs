using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Shared UIElements shell for WASM tool logs, progress, and trap reports.
    /// </summary>
    public sealed class ToolWindowShell : EditorWindow
    {
        static ToolWindowShell _instance;

        readonly List<string> _logLines = new();
        ScrollView _scroll;
        Label _status;
        Label _trap;
        ProgressBar _progress;

        [MenuItem("Window/Wasm Editor/Tool Shell")]
        public static void ShowWindow()
        {
            _instance = GetWindow<ToolWindowShell>("Wasm Tool Shell");
            _instance.minSize = new Vector2(420, 280);
        }

        public static ToolWindowShell Instance
        {
            get
            {
                if (_instance == null)
                    ShowWindow();
                return _instance;
            }
        }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;

            _status = new Label("Ready") { name = "status-label" };
            _progress = new ProgressBar { title = "Idle", value = 0 };
            _progress.style.marginTop = 6;
            _progress.style.marginBottom = 6;

            _scroll = new ScrollView { style = { flexGrow = 1 } };
            _trap = new Label { style = { whiteSpace = WhiteSpace.Normal, color = new Color(1f, 0.45f, 0.45f) } };

            root.Add(_status);
            root.Add(_progress);
            root.Add(new Label("Log") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            root.Add(_scroll);
            root.Add(new Label("Last Trap") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6 } });
            root.Add(_trap);

            RepaintLogs();
        }

        public void SetStatus(string text)
        {
            _status.text = text;
        }

        public void SetProgress(string title, float value)
        {
            _progress.title = title;
            _progress.value = value * 100f;
        }

        public void ClearProgress()
        {
            _progress.title = "Idle";
            _progress.value = 0;
        }

        public void AppendLog(string line)
        {
            _logLines.Add($"[{DateTime.Now:HH:mm:ss}] {line}");
            if (_logLines.Count > 500)
                _logLines.RemoveAt(0);
            RepaintLogs();
        }

        public void ShowTrap(TrapReport report)
        {
            _trap.text = report?.ToJson() ?? string.Empty;
            AppendLog($"TRAP: {report?.trapMessage}");
        }

        void RepaintLogs()
        {
            if (_scroll == null)
                return;

            _scroll.Clear();
            foreach (var line in _logLines)
                _scroll.Add(new Label(line) { style = { whiteSpace = WhiteSpace.Normal } });
        }
    }
}
