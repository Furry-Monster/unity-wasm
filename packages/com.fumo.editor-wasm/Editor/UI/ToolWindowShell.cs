using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Shared UIElements shell for WASM tool launcher, logs, progress, and trap reports.
    /// </summary>
    public sealed class ToolWindowShell : EditorWindow
    {
        const int MaxLogLines = 500;
        const int MaxPendingLogLines = 100;

        static ToolWindowShell _instance;
        static string _pendingStatus;
        static string _pendingTrapJson;
        static readonly List<string> _pendingLogLines = new();

        readonly List<string> _logLines = new();
        VisualElement _toolsList;
        ScrollView _scroll;
        Label _status;
        Label _trap;
        ProgressBar _progress;
        string _lastTrapJson = string.Empty;

        Action<ToolManifest> _onToolReloadedHandler;

        [MenuItem("Window/Wasm Editor/Tool Shell")]
        public static void ShowWindow()
        {
            _instance = GetWindow<ToolWindowShell>("Wasm Tool Shell");
            _instance.minSize = new Vector2(520, 360);
        }

        public static ToolWindowShell Instance => _instance;

        public static void NotifyStatus(string text)
        {
            _pendingStatus = text;
            _instance?.ApplyStatus(text);
        }

        public static void NotifyLog(string line)
        {
            _pendingLogLines.Add(line);
            while (_pendingLogLines.Count > MaxPendingLogLines)
                _pendingLogLines.RemoveAt(0);

            _instance?.ApplyLog(line);
        }

        public static void NotifyTrap(TrapReport report)
        {
            _pendingTrapJson = report?.ToJson() ?? string.Empty;
            _instance?.ApplyTrap(_pendingTrapJson, report?.trapMessage);
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

            var refreshBtn = new Button(WasmEditorRuntime.RefreshTools) { text = "Refresh Tools" };
            refreshBtn.style.marginBottom = 8;
            refreshBtn.style.alignSelf = Align.FlexStart;

            _toolsList = new VisualElement { name = "tools-list" };
            _toolsList.style.marginBottom = 8;

            _scroll = new ScrollView { style = { flexGrow = 1, minHeight = 80 } };
            _trap = new Label { style = { whiteSpace = WhiteSpace.Normal, color = new Color(1f, 0.45f, 0.45f) } };

            var trapHeaderRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 6 } };
            trapHeaderRow.Add(new Label("Last Trap") { style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } });
            trapHeaderRow.Add(new Button(CopyTrapJson) { text = "Copy JSON" });

            root.Add(_status);
            root.Add(_progress);
            root.Add(refreshBtn);
            root.Add(new Label("Tools") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            root.Add(_toolsList);
            root.Add(new Label("Log") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 4 } });
            root.Add(_scroll);
            root.Add(trapHeaderRow);
            root.Add(_trap);

            WasmEditorRuntime.ToolsChanged += RebuildToolsList;
            _onToolReloadedHandler = _ => RebuildToolsList();
            if (WasmEditorRuntime.HotReload != null)
                WasmEditorRuntime.HotReload.ToolReloaded += _onToolReloadedHandler;

            if (!string.IsNullOrEmpty(_pendingStatus))
                ApplyStatus(_pendingStatus);
            foreach (var line in _pendingLogLines)
                ApplyLog(line);
            _pendingLogLines.Clear();
            if (!string.IsNullOrEmpty(_pendingTrapJson))
                ApplyTrap(_pendingTrapJson, null);

            RebuildToolsList();
        }

        void OnDestroy()
        {
            WasmEditorRuntime.ToolsChanged -= RebuildToolsList;
            if (_onToolReloadedHandler != null && WasmEditorRuntime.HotReload != null)
                WasmEditorRuntime.HotReload.ToolReloaded -= _onToolReloadedHandler;
            if (_instance == this)
                _instance = null;
        }

        void RebuildToolsList()
        {
            if (_toolsList == null)
                return;

            _toolsList.Clear();
            var hotReload = WasmEditorRuntime.HotReload;

            if (WasmEditorRuntime.Tools.Count == 0)
            {
                _toolsList.Add(new Label("(No tools — Refresh Tools)") { style = { color = Color.gray } });
                return;
            }

            foreach (var tool in WasmEditorRuntime.Tools.OrderBy(t => t.name))
                _toolsList.Add(BuildToolRow(tool, hotReload));
        }

        static VisualElement BuildToolRow(ToolManifest tool, HotReloadService hotReload)
        {
            var card = new VisualElement
            {
                style =
                {
                    marginBottom = 6,
                    paddingBottom = 4,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f)
                }
            };

            var header = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
            };

            var nameLabel = new Label(tool.name) { style = { flexGrow = 1, minWidth = 120, unityFontStyleAndWeight = FontStyle.Bold } };
            var runBtn = new Button(() => WasmEditorRuntime.InvokeTool(tool.id)) { text = "Run" };
            runBtn.style.width = 48;

            var reloadLabel = new Label(FormatReloadTime(hotReload?.GetLastReloadUtc(tool.id)))
            {
                style = { width = 72, color = Color.gray, fontSize = 10, unityTextAlign = TextAnchor.MiddleRight }
            };

            header.Add(nameLabel);
            header.Add(runBtn);
            header.Add(reloadLabel);

            var meta = new Label(FormatToolMeta(tool))
            {
                style = { color = Color.gray, fontSize = 10, whiteSpace = WhiteSpace.Normal, marginTop = 2 }
            };
            meta.tooltip = tool.WasmPath;

            card.Add(header);
            card.Add(meta);
            return card;
        }

        static string FormatToolMeta(ToolManifest tool)
        {
            var abi = string.IsNullOrEmpty(tool.abi) ? "?" : tool.abi;
            var entry = string.IsNullOrEmpty(tool.entry) ? "?" : tool.entry;
            return $"{tool.id} · {abi} · {entry}";
        }

        static string FormatReloadTime(DateTime? utc)
        {
            if (!utc.HasValue)
                return "—";

            var elapsed = DateTime.UtcNow - utc.Value;
            if (elapsed.TotalMinutes < 1)
                return "just now";
            if (elapsed.TotalMinutes < 60)
                return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24)
                return $"{(int)elapsed.TotalHours}h ago";
            return utc.Value.ToLocalTime().ToString("MM-dd HH:mm");
        }

        void CopyTrapJson()
        {
            if (string.IsNullOrEmpty(_lastTrapJson))
                return;
            EditorGUIUtility.systemCopyBuffer = _lastTrapJson;
            ApplyLog("Trap JSON copied to clipboard.");
        }

        void ApplyStatus(string text)
        {
            if (_status != null)
                _status.text = text;
        }

        void ApplyLog(string line)
        {
            _logLines.Add($"[{DateTime.Now:HH:mm:ss}] {line}");
            while (_logLines.Count > MaxLogLines)
                _logLines.RemoveAt(0);
            RepaintLogs();
        }

        void ApplyTrap(string json, string shortMessage)
        {
            _lastTrapJson = json ?? string.Empty;
            if (_trap != null)
                _trap.text = _lastTrapJson;
            if (!string.IsNullOrEmpty(shortMessage))
                ApplyLog($"TRAP: {shortMessage}");
        }

        public void SetStatus(string text) => NotifyStatus(text);

        public void SetProgress(string title, float value)
        {
            if (_progress == null)
                return;
            _progress.title = title;
            _progress.value = value * 100f;
        }

        public void ClearProgress()
        {
            if (_progress == null)
                return;
            _progress.title = "Idle";
            _progress.value = 0;
        }

        public void AppendLog(string line) => NotifyLog(line);

        public void ShowTrap(TrapReport report) => NotifyTrap(report);

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
