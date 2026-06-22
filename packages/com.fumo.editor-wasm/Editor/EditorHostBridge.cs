using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Wasmtime;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Implements editor-api host imports exposed to WASM tools.
    /// </summary>
    public sealed class EditorHostBridge
    {
        readonly HandlePool<UnityEngine.Object> _objectHandles = new();
        readonly HostCallTrace _trace;
        Memory _guestMemory;

        public HandlePool<UnityEngine.Object> ObjectHandles => _objectHandles;

        public EditorHostBridge(HostCallTrace trace)
        {
            _trace = trace;
        }

        public void SetGuestMemory(Memory memory) => _guestMemory = memory;

        public void ClearHandles() => _objectHandles.Clear();

        public void RegisterImports(Linker linker)
        {
            RegisterCoreImports(linker);
            RegisterSelectionImports(linker);
            RegisterAssetImports(linker);
        }

        void RegisterCoreImports(Linker linker)
        {
            linker.DefineFunction("editor_core", "log", (Caller caller, int level, int ptr, int len) =>
            {
                Trace("editor_core", "log");
                var message = WasmMemoryBridge.ReadString(GetMemory(caller), ptr, len);
                switch (level)
                {
                    case 1:
                        Debug.LogWarning(message);
                        ToolWindowShell.NotifyLog($"WARN: {message}");
                        break;
                    case 2:
                        Debug.LogError(message);
                        ToolWindowShell.NotifyLog($"ERROR: {message}");
                        break;
                    default:
                        Debug.Log(message);
                        ToolWindowShell.NotifyLog(message);
                        break;
                }
            });

            linker.DefineFunction("editor_core", "log_error", (Caller caller, int ptr, int len) =>
            {
                Trace("editor_core", "log_error");
                var message = WasmMemoryBridge.ReadString(GetMemory(caller), ptr, len);
                Debug.LogError(message);
                ToolWindowShell.NotifyLog($"ERROR: {message}");
            });

            linker.DefineFunction("editor_core", "get_editor_time", () =>
            {
                Trace("editor_core", "get_editor_time");
                return EditorApplication.timeSinceStartup;
            });

            linker.DefineFunction("editor_core", "show_progress", (Caller caller, int titlePtr, int titleLen, int infoPtr, int infoLen, float progress) =>
            {
                Trace("editor_core", "show_progress");
                var memory = GetMemory(caller);
                var title = WasmMemoryBridge.ReadString(memory, titlePtr, titleLen);
                var info = WasmMemoryBridge.ReadString(memory, infoPtr, infoLen);
                EditorUtility.DisplayProgressBar(title, info, progress);
                ToolWindowShell.NotifyProgress(title, info, progress);
            });

            linker.DefineFunction("editor_core", "clear_progress", () =>
            {
                Trace("editor_core", "clear_progress");
                EditorUtility.ClearProgressBar();
                ToolWindowShell.NotifyClearProgress();
            });
        }

        void RegisterSelectionImports(Linker linker)
        {
            linker.DefineFunction("editor_selection", "get_active_object", () =>
            {
                Trace("editor_selection", "get_active_object");
                _objectHandles.Sweep();
                return (long)_objectHandles.Register(Selection.activeObject);
            });

            linker.DefineFunction("editor_selection", "get_active_objects_count", () =>
            {
                Trace("editor_selection", "get_active_objects_count");
                return Selection.objects?.Length ?? 0;
            });

            linker.DefineFunction("editor_selection", "get_active_object_at", (int index) =>
            {
                Trace("editor_selection", "get_active_object_at");
                _objectHandles.Sweep();
                var objects = Selection.objects;
                if (objects == null || index < 0 || index >= objects.Length)
                    return 0L;
                return (long)_objectHandles.Register(objects[index]);
            });

            linker.DefineFunction("editor_selection", "get_active_asset_path", (Caller caller, int outPtr, int maxLen) =>
            {
                Trace("editor_selection", "get_active_asset_path");
                var path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (string.IsNullOrEmpty(path))
                    return 0;

                return WasmMemoryBridge.WriteString(GetMemory(caller), outPtr, maxLen, path);
            });

            linker.DefineFunction("editor_selection", "get_object_name", (Caller caller, long handle, int outPtr, int maxLen) =>
            {
                Trace("editor_selection", "get_object_name");
                if (!_objectHandles.TryGet((ulong)handle, out var obj) || obj == null)
                    return -1;

                return WasmMemoryBridge.WriteString(GetMemory(caller), outPtr, maxLen, obj.name);
            });
        }

        void RegisterAssetImports(Linker linker)
        {
            linker.DefineFunction("editor_assets", "asset_exists", (Caller caller, int pathPtr, int pathLen) =>
            {
                Trace("editor_assets", "asset_exists");
                var path = WasmMemoryBridge.ReadString(GetMemory(caller), pathPtr, pathLen);
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null ? 1 : 0;
            });

            linker.DefineFunction("editor_assets", "find_assets_count", (Caller caller, int filterPtr, int filterLen, int pathsPtr, int pathsLen) =>
            {
                Trace("editor_assets", "find_assets_count");
                var memory = GetMemory(caller);
                var filter = WasmMemoryBridge.ReadString(memory, filterPtr, filterLen);
                var searchPaths = ParseNullSeparatedPaths(memory, pathsPtr, pathsLen);
                return AssetDatabase.FindAssets(filter, searchPaths.ToArray()).Length;
            });

            linker.DefineFunction("editor_assets", "find_asset_at", (Caller caller, int filterPtr, int filterLen, int pathsPtr, int pathsLen, int index, int outPtr, int maxLen) =>
            {
                Trace("editor_assets", "find_asset_at");
                var memory = GetMemory(caller);
                var filter = WasmMemoryBridge.ReadString(memory, filterPtr, filterLen);
                var searchPaths = ParseNullSeparatedPaths(memory, pathsPtr, pathsLen);
                var guids = AssetDatabase.FindAssets(filter, searchPaths.ToArray());
                if (index < 0 || index >= guids.Length)
                    return -1;

                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                return WasmMemoryBridge.WriteString(memory, outPtr, maxLen, path);
            });

            linker.DefineFunction("editor_assets", "load_text_asset", (Caller caller, int pathPtr, int pathLen, int outPtr, int maxLen) =>
            {
                Trace("editor_assets", "load_text_asset");
                var memory = GetMemory(caller);
                var path = WasmMemoryBridge.ReadString(memory, pathPtr, pathLen);
                try
                {
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (textAsset == null)
                        return -1;
                    return WasmMemoryBridge.WriteString(memory, outPtr, maxLen, textAsset.text);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"load_text_asset failed: {ex.Message}");
                    return -1;
                }
            });

            linker.DefineFunction("editor_assets", "write_bulk_payload", (Caller caller, int offset, int payloadPtr, int payloadLen, int bulkType) =>
            {
                Trace("editor_assets", "write_bulk_payload");
                var memory = GetMemory(caller);
                WasmMemoryBridge.WriteBulkHeader(memory, offset, (ushort)bulkType, (uint)payloadLen);
                var dest = offset + WasmMemoryBridge.BulkHeaderSize;
                if (payloadLen > 0)
                {
                    memory.GetSpan(payloadPtr, payloadLen).CopyTo(memory.GetSpan(dest, payloadLen));
                }
                return payloadLen + WasmMemoryBridge.BulkHeaderSize;
            });
        }

        static List<string> ParseNullSeparatedPaths(Memory memory, int ptr, int len)
        {
            var raw = WasmMemoryBridge.ReadString(memory, ptr, len);
            var paths = new List<string>();
            foreach (var part in raw.Split('\0', StringSplitOptions.RemoveEmptyEntries))
                paths.Add(part);
            if (paths.Count == 0)
                paths.Add("Assets");
            return paths;
        }

        Memory GetMemory(Caller caller)
        {
            if (_guestMemory != null)
                return _guestMemory;

            var memory = caller.GetMemory("memory");
            if (memory != null)
            {
                _guestMemory = memory;
                return memory;
            }

            throw new InvalidOperationException("Guest module does not export linear memory.");
        }

        void Trace(string module, string name) => _trace.Record(module, name);
    }
}
